(function () {
  "use strict";

  var ICONS = {
    draft: '<svg class="icon" viewBox="0 0 24 24"><path d="M20 15a2 2 0 0 1-2 2H8l-4 4V5a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v10Z"></path></svg>',
    save: '<svg class="icon" viewBox="0 0 24 24"><path d="M5 4h11l3 3v13H5V4Z"></path><path d="M8 4v5h7V4"></path><path d="M8 14h8v6H8z"></path></svg>',
    dup: '<svg class="icon" viewBox="0 0 24 24"><rect x="4" y="4" width="9" height="9" rx="1.5"></rect><rect x="11" y="11" width="9" height="9" rx="1.5"></rect></svg>',
    scan: '<svg class="icon" viewBox="0 0 24 24"><path d="M4 8V5.5A1.5 1.5 0 0 1 5.5 4H8"></path><path d="M16 4h2.5A1.5 1.5 0 0 1 20 5.5V8"></path><path d="M20 16v2.5a1.5 1.5 0 0 1-1.5 1.5H16"></path><path d="M8 20H5.5A1.5 1.5 0 0 1 4 18.5V16"></path><path d="M7 9v6M11 9v6M14 9v6M17 9v6"></path></svg>'
  };

  // "command" is the Thai word the backend command parser matches; "label" is what the button shows.
  var SHORTCUTS = [
    { icon: "draft", label: "Draft customer reply", command: "ร่างตอบลูกค้า", key: "draft" },
    { icon: "save", label: "Save case note", command: "บันทึกเคส", key: "save" },
    { icon: "dup", label: "Find duplicates", command: "เคสซ้ำ", key: "duplicate" },
    { icon: "scan", label: "View raw #ticketId", command: "", key: "raw" }
  ];

  var SAVE_CASE_COMMAND = SHORTCUTS.filter(function (s) { return s.key === "save"; })[0].command;

  // Mirrors the backend command parser's own match rule (ChatCommandParser.Parse: exact match, or
  // the command word as a prefix with more text after it) - only used client-side to decide
  // whether the answer that comes back should get the "บันทึกเคส" save button (see submitAsk).
  function isSaveCaseCommand(text) {
    var t = (text || "").trim();
    return t === SAVE_CASE_COMMAND || t.indexOf(SAVE_CASE_COMMAND) === 0;
  }

  var state = {
    conversationId: null,
    isLoading: false,
    abortController: null,  // aborts the in-flight Search/Ask fetch when the user presses Stop
    messages: [],
    caseCache: {},          // id -> CasePreview
    lastSearchCases: [],
    relevantScripts: [],    // KnownScriptPreview[] from the latest Search/Ask response
    activeCaseId: null,
    rightTab: "sql",
    rightFresh: false,
    scriptPlaceholders: {}, // scriptName -> { varName: value }
    scriptRunResults: {},   // scriptName -> { loading, error, tables }
    scriptEdits: {},        // scriptName -> { text, original, risk } - SQL edited and run from the SQL editor
    mobileView: "left",
    hasShownSearchPreview: false
  };

  var chatStreamEl = document.getElementById("chatStream");
  var quickCommandBarEl = document.getElementById("quickCommandBar");
  var chatInputEl = document.getElementById("chatInput");
  var chatSendBtnEl = document.querySelector("#chatInputForm button[type=submit]");
  var rightTabsEl = document.getElementById("rightTabs");
  var rightContentEl = document.getElementById("rightContent");
  var workspaceEl = document.getElementById("workspace");
  var mobileFreshDotEl = document.getElementById("mobileFreshDot");
  var toastEl = document.getElementById("toast");
  var caseDrawerEl = document.getElementById("caseDrawer");
  var drawerHeadTextEl = document.getElementById("drawerHeadText");
  var drawerBodyEl = document.getElementById("drawerBody");

  var newChatBtnEl = document.getElementById("newChatBtn");
  var chatStopBtnEl = document.getElementById("chatStopBtn");

  var toastTimer = null;
  function showToast(msg) {
    toastEl.textContent = msg;
    toastEl.classList.add("show");
    clearTimeout(toastTimer);
    toastTimer = setTimeout(function () { toastEl.classList.remove("show"); }, 2600);
  }

  function escapeHtml(s) {
    return String(s).replace(/[&<>]/g, function (c) { return c === "&" ? "&amp;" : c === "<" ? "&lt;" : "&gt;"; });
  }

  function uniq(arr) {
    return arr.filter(function (v, i) { return arr.indexOf(v) === i; });
  }

  /* =========================== API =========================== */

  async function postJson(handler, question, signal) {
    var res = await fetch("/Index?handler=" + handler, {
      method: "POST",
      signal: signal,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question: question, conversationId: state.conversationId })
    });
    if (!res.ok) throw new Error("HTTP " + res.status);
    return res.json();
  }

  // Streaming variant of postJson("Ask", ...): the server sends newline-delimited JSON while the
  // model is still writing. Each {"type":"delta","html":...} is the answer so far (rendered
  // server-side) and goes to onPartial; {"type":"done","response":...} carries the same object the
  // plain Ask handler returns, which is what this resolves with.
  async function postAskStream(question, signal, onPartial) {
    var res = await fetch("/Index?handler=AskStream", {
      method: "POST",
      signal: signal,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question: question, conversationId: state.conversationId })
    });
    if (!res.ok) throw new Error("HTTP " + res.status);

    var reader = res.body.getReader();
    var decoder = new TextDecoder("utf-8");
    var buffer = "";
    for (;;) {
      var chunk = await reader.read();
      if (chunk.done) break;
      buffer += decoder.decode(chunk.value, { stream: true });
      var newline;
      while ((newline = buffer.indexOf("\n")) >= 0) {
        var line = buffer.slice(0, newline);
        buffer = buffer.slice(newline + 1);
        if (!line.trim()) continue;
        var message = JSON.parse(line);
        if (message.type === "delta") onPartial(message.html);
        else if (message.type === "done") return message.response;
      }
    }
    throw new Error("คำตอบถูกตัดก่อนจบ");
  }

  // sqlText runs runs raw AI-authored SQL instead of a whitelisted script by name - pass exactly
  // one of scriptName/sqlText (see Index.cshtml.cs.SqlRunRequest).
  async function postSqlRun(scriptName, sqlText, parameters) {
    var res = await fetch("/Index?handler=RunSql", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ scriptName: scriptName || null, sqlText: sqlText || null, parameters: parameters })
    });
    if (!res.ok) throw new Error("HTTP " + res.status);
    return res.json();
  }

  /* =========================== Chat stream =========================== */

  function appendMessage(role, html, opts) {
    var msg = { role: role, html: html, empty: !!(opts && opts.empty), error: !!(opts && opts.error), loading: !!(opts && opts.loading), streaming: !!(opts && opts.streaming) };
    state.messages.push(msg);
    renderChatStream();
    return msg;
  }

  function removeMessage(msg) {
    var idx = state.messages.indexOf(msg);
    if (idx !== -1) state.messages.splice(idx, 1);
    renderChatStream();
  }

  // stickToBottomOnly: used while an answer is streaming in - re-render on every piece, but only
  // follow the growing text if the reader is already at the bottom (scrolling up to re-read
  // something must not get yanked back down every 120 ms). Everything else always jumps to the end.
  function renderChatStream(stickToBottomOnly) {
    var wasNearBottom = chatStreamEl.scrollHeight - chatStreamEl.scrollTop - chatStreamEl.clientHeight < 120;
    if (!state.messages.length) {
      chatStreamEl.innerHTML =
        '<div class="msg msg-system msg-empty"><div class="msg-bubble">' +
          "พิมพ์อาการที่เจอ, เลข Loading No., หรือวาง Error message ด้านล่าง — ระบบจะค้นจากเคสเก่าที่ปิดงานแล้ว 2,180 เคส แล้วให้ AI วิเคราะห์ทางขวาให้อัตโนมัติ" +
        "</div></div>";
    } else {
      chatStreamEl.innerHTML = state.messages.map(function (m) {
        var cls = "msg msg-" + m.role + (m.error ? " msg-error" : "") + (m.streaming ? " msg-streaming" : "");
        var body = m.loading
          ? '<span class="loading-dots"><span></span><span></span><span></span></span>'
          : m.html + (m.isSaveCase && !m.streaming
              ? '<div class="save-case-row"><button type="button" class="btn-primary save-case-btn" data-save-case>บันทึกเคส</button></div>'
              : "");
        return '<div class="' + cls + '"><div class="msg-bubble">' + body + "</div></div>";
      }).join("");
    }
    restoreRunResults();
    restoreSqlEdits();
    bindMessageInteractions();
    if (!stickToBottomOnly || wasNearBottom) chatStreamEl.scrollTop = chatStreamEl.scrollHeight;
    persistSession();
  }

  function bindMessageInteractions() {
    Array.prototype.forEach.call(chatStreamEl.querySelectorAll("[data-open-case]"), function (btn) {
      btn.addEventListener("click", function () {
        // Case ids are strings now ("13525" for a real case, "AI-1" for a team-added one) -
        // Number() used to be safe when every id was numeric, but it turns "AI-1" into NaN.
        openCaseDrawer(btn.getAttribute("data-open-case"));
      });
    });
    Array.prototype.forEach.call(chatStreamEl.querySelectorAll("[data-narrow]"), function (btn) {
      btn.addEventListener("click", function () {
        handleNarrow(btn.getAttribute("data-narrow"), btn.textContent.trim());
      });
    });
    // Same positional lookup as locateRunSlot: chatStreamEl.children[idx] <-> state.messages[idx],
    // rebuilt on every render, so this always finds the message the button actually belongs to.
    Array.prototype.forEach.call(chatStreamEl.querySelectorAll("[data-save-case]"), function (btn) {
      btn.addEventListener("click", function () {
        var msgEl = btn.closest(".msg");
        var idx = Array.prototype.indexOf.call(chatStreamEl.children, msgEl);
        var msg = state.messages[idx];
        if (msg) openSaveCaseEditor(msg);
      });
    });
  }

  function renderCasePills(ids) {
    return ids.map(function (id) {
      return '<button type="button" class="case-pill" data-open-case="' + id + '">#' + id + "</button>";
    }).join(" ");
  }

  function cacheCases(cases) {
    cases.forEach(function (c) { state.caseCache[c.id] = c; });
  }

  function computeModuleChips(cases) {
    var modules = uniq(cases.map(function (c) { return c.bsModule; }).filter(Boolean));
    return modules.length > 1 ? modules.slice(0, 3) : [];
  }

  function handleNarrow(module, label) {
    var narrowed = state.lastSearchCases.filter(function (c) { return c.bsModule === module; });
    appendMessage("user", escapeHtml(label));
    appendMessage("system", "ตัดตัวเลือกแล้ว เหลือ " + narrowed.length + " เคสที่ตรง: " + renderCasePills(narrowed.map(function (c) { return c.id; })));
  }

  /* =========================== Ask / search =========================== */

  function setLoading(loading) {
    state.isLoading = loading;
    if (newChatBtnEl) newChatBtnEl.disabled = loading;
    if (chatStopBtnEl) chatStopBtnEl.hidden = !loading;
    if (chatSendBtnEl) chatSendBtnEl.hidden = loading;
    chatInputEl.disabled = loading;
    if (chatSendBtnEl) chatSendBtnEl.disabled = loading || !chatInputEl.value.trim();
    Array.prototype.forEach.call(quickCommandBarEl.querySelectorAll(".qc-btn"), function (b) { b.disabled = loading; });
  }

  // displayText (optional) is what the user bubble shows instead of the full text sent to the
  // AI - used when a live-run result is auto-sent, so the chat doesn't fill with raw rows.
  async function submitAsk(raw, displayText) {
    var text = (raw || "").trim();
    if (!text || state.isLoading) return;
    var isSaveCase = isSaveCaseCommand(text);

    appendMessage("user", escapeHtml(displayText || text));
    setLoading(true);
    var loadingMsg = appendMessage("system", "", { loading: true });
    var askLoadingMsg = null;
    var streamMsg = null;
    var controller = new AbortController();
    state.abortController = controller;

    try {
      var search = await postJson("Search", text, controller.signal);
      state.conversationId = search.conversationId;
      cacheCases(search.cases);
      state.lastSearchCases = search.cases;
      state.relevantScripts = search.relevantScripts || [];
      removeMessage(loadingMsg);

      if (search.isFinal) {
        appendMessage("system", search.finalAnswerHtml || "");
        if (search.cases.length === 1) {
          state.activeCaseId = search.cases[0].id;
        }
        state.rightTab = search.cases.length === 1 ? "context" : "sql";
        markRightFresh();
        renderRightTabs();
        renderRightContent();
        setLoading(false);
        return;
      }

      // Only show the "found N cases" / "no cases" preview on the first turn of a conversation -
      // every later turn re-runs Search too (RelevantScripts/case cache still need refreshing),
      // but repeating this line on every follow-up message is just noise once the user is already
      // mid-conversation and knows what's happening.
      if (!state.hasShownSearchPreview) {
        state.hasShownSearchPreview = true;

        if (search.cases.length === 0) {
          appendMessage(
              "system",
            'ระบบค้นจากเคสเก่าที่ปิดงานแล้ว ไม่มีเคสที่ระบุเรื่องนี้โดยตรง — กำลังให้ AI ช่วยแนะนำแนวทางทั่วไปต่อ…'
          );
        } else {
          var chips = computeModuleChips(search.cases);
          var chipsHtml = chips.length
            ? '<div class="quick-chip-row">' + chips.map(function (m) {
                return '<button type="button" class="quick-chip" data-narrow="' + escapeHtml(m) + '">' + escapeHtml(m) + "</button>";
              }).join("") + "</div>"
            : "";
          appendMessage(
            "system",
            "พบเคสที่เกี่ยวข้อง " + search.cases.length + " เคส: " + renderCasePills(search.cases.map(function (c) { return c.id; })) +
              "<br>กำลังให้ AI สรุปคำตอบ…" + chipsHtml
          );
        }
      }

      askLoadingMsg = appendMessage("system", "", { loading: true });
      markRightFresh();
      renderRightTabs();
      renderRightContent();

      // The answer grows in the chat as the model writes it: the loading dots are swapped for a
      // message on the first piece, which is then updated in place on each following piece (the
      // buttons inside it are inert until it is complete - see .msg-streaming in app.css).
      var ask = await postAskStream(text, controller.signal, function (partialHtml) {
        if (!streamMsg) {
          removeMessage(askLoadingMsg);
          askLoadingMsg = null;
          streamMsg = appendMessage("system", partialHtml, { streaming: true });
        } else {
          streamMsg.html = partialHtml;
          renderChatStream(true);
        }
      });
      state.conversationId = ask.conversationId;
      state.relevantScripts = ask.relevantScripts || [];

      // The "บันทึกเคส" save button (opens saveCaseEditor) only ever goes on the answer to that
      // exact command - see renderChatStream, which reads these fields back off the message.
      var saveCaseInfo = isSaveCase ? {
        conversationId: ask.conversationId,
        referencedCaseIds: ask.referencedCaseIds || [],
        knownScripts: (ask.relevantScripts || []).map(function (s) { return s.name; })
      } : null;

      if (streamMsg) {
        // Same message, now with the fully rendered answer (Run live buttons, case pills, ...).
        streamMsg.html = ask.answerHtml;
        streamMsg.rawText = ask.answer;
        streamMsg.isSaveCase = isSaveCase;
        streamMsg.saveCaseInfo = saveCaseInfo;
        streamMsg.streaming = false;
        streamMsg.error = false;
        renderChatStream();
      } else {
        // Nothing streamed (e.g. an error answer) - show the answer the usual way.
        removeMessage(askLoadingMsg);
        askLoadingMsg = null;
        var finalMsg = appendMessage("system", ask.answerHtml);
        finalMsg.rawText = ask.answer;
        finalMsg.isSaveCase = isSaveCase;
        finalMsg.saveCaseInfo = saveCaseInfo;
        renderChatStream();
      }

      markRightFresh();
      renderRightTabs();
      renderRightContent();
    } catch (err) {
      removeMessage(loadingMsg);
      if (askLoadingMsg) removeMessage(askLoadingMsg);
      if (streamMsg) removeMessage(streamMsg);
      if (err && err.name === "AbortError") {
        // The user pressed Stop. Nothing was stored for this turn server-side (history is only
        // appended once an answer exists), so the conversation continues from the last answer.
        appendMessage("system", "หยุดคำถามนี้แล้ว — พิมพ์คำถามใหม่หรือถามซ้ำได้เลย");
        renderRightContent();
        return;
      }
      appendMessage("system", "เกิดข้อผิดพลาดในการเชื่อมต่อ: " + escapeHtml(err.message || String(err)), { error: true });
      renderRightContent();
    } finally {
      if (state.abortController === controller) state.abortController = null;
      setLoading(false);
    }
  }

  chatStopBtnEl.addEventListener("click", function () {
    if (state.abortController) state.abortController.abort();
  });

  document.getElementById("chatInputForm").addEventListener("submit", function (e) {
    e.preventDefault();
    var v = chatInputEl.value;
    chatInputEl.value = "";
    resizeChatInput();
    if (chatSendBtnEl) chatSendBtnEl.disabled = true;
    submitAsk(v);
  });

  // chatInput is a <textarea> (not a single-line <input>) so it can actually hold and show
  // multi-line content - a plain <input> silently drops every "\n" written into it, which used to
  // turn a staged SQL result (or several staged one after another - see bindSqlSendToAiButton)
  // into one unreadable run-on line with no way to tell where one block ends and the next begins.
  // Grows with the content up to a cap (CSS max-height) instead of a fixed number of rows, and
  // resizeChatInput() must be called after every *programmatic* value change too, since only real
  // user typing fires the "input" event a <textarea> auto-sizes on.
  function resizeChatInput() {
    chatInputEl.style.height = "auto";
    chatInputEl.style.height = chatInputEl.scrollHeight + "px";
  }

  chatInputEl.addEventListener("input", function () {
    if (chatSendBtnEl) chatSendBtnEl.disabled = state.isLoading || !chatInputEl.value.trim();
    resizeChatInput();
  });

  // A <textarea> never submits its form on Enter by itself (unlike the <input> this replaced) -
  // restore that as the default, with the usual chat-app escape hatch (Shift+Enter, or while an
  // IME composition is in progress) to actually type a newline instead.
  chatInputEl.addEventListener("keydown", function (e) {
    if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
      e.preventDefault();
      document.getElementById("chatInputForm").requestSubmit();
    }
  });

  if (chatSendBtnEl) chatSendBtnEl.disabled = true;

  /* =========================== Quick command bar =========================== */

  function renderQuickCommandBar() {
    quickCommandBarEl.innerHTML = SHORTCUTS.map(function (s) {
      return '<button type="button" class="qc-btn" data-key="' + s.key + '">' + ICONS[s.icon] + "<span>" + s.label + "</span></button>";
    }).join("");

    Array.prototype.forEach.call(quickCommandBarEl.querySelectorAll(".qc-btn"), function (btn) {
      btn.addEventListener("click", function () {
        var key = btn.getAttribute("data-key");
        // The button shows an English label, but the backend command parser matches the Thai command word.
        var label = SHORTCUTS.filter(function (s) { return s.key === key; })[0].command;

        if (key === "raw") {
          chatInputEl.focus();
          chatInputEl.value = "#";
          resizeChatInput();
          if (chatSendBtnEl) chatSendBtnEl.disabled = false;
          showToast("พิมพ์เลขเคสต่อจาก # แล้ว Enter เช่น #13525");
          return;
        }

        // Same behavior as the real command shortcuts already had: if there's text in the box,
        // prefix the command word onto it; if the box is empty, send the bare command right
        // away. The backend resolves what it means (reuse last turn, etc.) - the client doesn't
        // need to know or gate on conversation state itself.
        if (chatInputEl.value.trim()) {
          chatInputEl.value = label + " " + chatInputEl.value;
          resizeChatInput();
          chatInputEl.focus();
          if (chatSendBtnEl) chatSendBtnEl.disabled = state.isLoading || !chatInputEl.value.trim();
        } else {
          submitAsk(label);
        }
      });
    });
  }

  /* =========================== Right panel: tabs =========================== */

  var RIGHT_TABS = [
    { key: "context", label: "Case Context" },
    { key: "sql", label: "SQL Studio" }
  ];

  function markRightFresh() { state.rightFresh = true; }

  function isMobileLayout() { return window.matchMedia("(max-width: 900px)").matches; }
  function isRightCollapsed() { return workspaceEl.getAttribute("data-right-collapsed") === "true"; }
  function setRightCollapsed(collapsed) {
    workspaceEl.setAttribute("data-right-collapsed", collapsed ? "true" : "false");
    renderRightTabs();
  }

  function renderRightTabs() {
    rightTabsEl.innerHTML = RIGHT_TABS.map(function (t) {
      var selected = state.rightTab === t.key && !isRightCollapsed();
      return '<button type="button" class="right-tab" data-tab="' + t.key + '" aria-selected="' + selected + '">' + t.label + '<span class="rt-dot"></span></button>';
    }).join("");

    Array.prototype.forEach.call(rightTabsEl.querySelectorAll(".right-tab"), function (btn) {
      btn.addEventListener("click", function () {
        var tab = btn.getAttribute("data-tab");
        // Desktop: clicking a tab while the panel is collapsed expands it onto that tab; clicking
        // the already-active tab while expanded collapses it. (On the mobile layout the panel is
        // a full-screen switch instead, so tabs there only ever select.)
        if (!isMobileLayout()) {
          if (isRightCollapsed()) {
            setRightCollapsed(false);
          } else if (tab === state.rightTab) {
            setRightCollapsed(true);
            return;
          }
        }
        state.rightTab = tab;
        state.rightFresh = false;
        renderRightTabs();
        renderRightContent();
        setMobileView("right");
      });
    });

    mobileFreshDotEl.style.display = state.rightFresh ? "inline-block" : "none";
  }

  function renderRightContent() {
    if (state.rightTab === "context") { renderCaseContext(); persistSession(); return; }
    if (state.rightTab === "sql") { renderSqlStudio(); persistSession(); return; }
  }

  /* =========================== Case context =========================== */

  function openCaseContext(id) {
    if (!state.caseCache[id]) {
      showToast("ยังไม่มีเนื้อเคส #" + id + " ในเทิร์นนี้ — ลองพิมพ์ #" + id + " เพื่อดึงเคสนี้โดยตรงก่อนครับ");
      return;
    }
    state.activeCaseId = id;
    state.rightTab = "context";
    markRightFresh();
    renderRightTabs();
    renderRightContent();
    setMobileView("right");
  }

  function renderCaseContext() {
    var c = state.caseCache[state.activeCaseId];
    if (!c) {
      rightContentEl.innerHTML = '<div class="right-empty">คลิกเลขเคส (เช่น <span class="mono">#13525</span>) ในแชทเพื่อดูรายละเอียดที่นี่</div>';
      return;
    }

    rightContentEl.innerHTML =
      '<div class="context-card">' +
      '<div class="context-head"><div><h3 class="mono">#' + c.id + "</h3><p>" + escapeHtml(c.subCategory || c.caseType || "") + "</p></div><span class=\"module-chip\">" + escapeHtml(c.bsModule) + "</span></div>" +
      '<div class="context-section"><h4>ปัญหา</h4><div class="body">' + escapeHtml(c.summary) + "</div></div>" +
      '<div class="context-section"><h4>รายละเอียด</h4><div class="body">' + escapeHtml(c.description || "") + "</div></div>" +
      '<div class="context-section"><h4>วิธีแก้ที่บันทึกไว้</h4><div class="body">' + escapeHtml(c.stepsToReproduce || "") + "</div></div>" +
      '<div class="context-section"><h4>ข้อมูลเพิ่มเติม</h4><div class="body">' + escapeHtml(c.additionalInformation || "") + "</div></div>" +
      '<div class="context-section"><h4>บันทึกเพิ่มเติม</h4><div class="body">' + escapeHtml(c.notes || "") + "</div></div>" +
      '<div class="context-section"><h4>เนื้อหาเคส (เต็ม)</h4><div class="body mono">' + escapeHtml(c.kbContent) + "</div></div>" +
      (state.relevantScripts.length ? '<div class="context-section"><button type="button" class="btn-primary" id="ctxGoSqlBtn"><svg class="icon" viewBox="0 0 24 24" style="width:14px;height:14px"><path d="M5 4h11l3 3v13H5V4Z"></path><path d="M8 4v5h7V4"></path></svg>Go to SQL Studio</button></div>' : "") +
      "</div>";

    var goSqlBtn = document.getElementById("ctxGoSqlBtn");
    if (goSqlBtn) goSqlBtn.addEventListener("click", function () {
      state.rightTab = "sql";
      renderRightTabs();
      renderRightContent();
    });
  }

  /* =========================== Reference Case Drawer =========================== */

  function openCaseDrawer(id) {
    var c = state.caseCache[id];
    if (!c) {
      showToast("ยังไม่มีเนื้อเคส #" + id + " ในเทิร์นนี้ — ลองพิมพ์ #" + id + " เพื่อดึงเคสนี้โดยตรงก่อนครับ");
      return;
    }

    drawerHeadTextEl.innerHTML = '<h3 class="mono">#' + c.id + "</h3><p>" + escapeHtml(c.bsModule) + " · " + escapeHtml(c.subCategory || c.caseType || "") + "</p>";
    drawerBodyEl.innerHTML =
      '<div class="context-section"><h4>ปัญหา</h4><div class="body">' + escapeHtml(c.summary) + "</div></div>" +
      '<div class="context-section"><h4>เนื้อหาเคส (เต็ม)</h4><div class="body mono">' + escapeHtml(c.kbContent) + "</div></div>" +
      '<div class="context-section drawer-actions">' +
        '<button type="button" class="btn-secondary" id="drawerPinBtn">Pin to Case Context tab</button>' +
        (state.relevantScripts.length ? '<button type="button" class="btn-primary" id="drawerSqlBtn"><svg class="icon" viewBox="0 0 24 24" style="width:14px;height:14px"><path d="M5 4h11l3 3v13H5V4Z"></path><path d="M8 4v5h7V4"></path></svg>Go to SQL Studio</button>' : "") +
      "</div>";

    document.getElementById("drawerPinBtn").addEventListener("click", function () {
      closeCaseDrawer();
      openCaseContext(id);
    });
    var drawerSqlBtn = document.getElementById("drawerSqlBtn");
    if (drawerSqlBtn) drawerSqlBtn.addEventListener("click", function () {
      closeCaseDrawer();
      state.rightTab = "sql";
      renderRightTabs();
      renderRightContent();
      setMobileView("right");
    });

    caseDrawerEl.setAttribute("data-open", "true");
    caseDrawerEl.setAttribute("aria-hidden", "false");
  }

  function closeCaseDrawer() {
    caseDrawerEl.setAttribute("data-open", "false");
    caseDrawerEl.setAttribute("aria-hidden", "true");
  }

  document.getElementById("drawerBackdrop").addEventListener("click", closeCaseDrawer);
  document.getElementById("drawerCloseBtn").addEventListener("click", closeCaseDrawer);
  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape" && caseDrawerEl.getAttribute("data-open") === "true") closeCaseDrawer();
  });

  /* =========================== SQL Studio (real Known Scripts) =========================== */

  // Anchored to the start of a line so a commented-out "--DECLARE @x = ''" is not mistaken for a
  // real variable (the server's own DeclareLine regex is anchored the same way).
  var DECLARE_RE = /^[ \t]*DECLARE\s+@(\w+)\s+\w+(?:\([^)]*\))?\s*=\s*'([^']*)'/gim;
  var WRITE_RE = /\b(DELETE|UPDATE|INSERT)\b/i;
  var MOCS_QUALIFIER_RE = /\[MoCS\]\.(?=\[)/gi;

  // Every KnownScripts/*.sql file hardcodes "[MoCS]." in front of its table names (e.g.
  // "[MoCS].[dbo].[Plan]"), assuming the real production database is literally named "MoCS".
  // The dev/demo database this points at is actually named "MoCS_dev" - pasting the script as-is
  // into SSMS while connected to MoCS_dev fails with "Invalid object name" because SQL Server
  // reads "[MoCS]" as a different database. Stripped here (both for on-screen display and for
  // what Copy puts on the clipboard) so a copy-pasted script works regardless of what the
  // connected database happens to be named - mirrors the same stripping SqlScriptRunner already
  // does server-side before a live "รันจริง" execution.
  function stripMocsQualifier(sqlContent) {
    return sqlContent.replace(MOCS_QUALIFIER_RE, "");
  }

  function detectPlaceholders(sqlContent) {
    var vars = [];
    var re = new RegExp(DECLARE_RE.source, "gim");
    var m;
    while ((m = re.exec(sqlContent)) !== null) {
      vars.push({ name: m[1], defaultValue: m[2] });
    }
    return vars;
  }

  function ensurePlaceholderState(script) {
    if (!state.scriptPlaceholders[script.name]) {
      var values = {};
      detectPlaceholders(script.sqlContent).forEach(function (p) { values[p.name] = p.defaultValue; });
      state.scriptPlaceholders[script.name] = values;
    }
    return state.scriptPlaceholders[script.name];
  }

  function renderScriptText(script, values) {
    var lines = stripMocsQualifier(script.sqlContent).replace(/\r\n/g, "\n").split("\n");
    return lines.map(function (line) {
      var m = /^(\s*DECLARE\s+@(\w+)\s+\w+(?:\([^)]*\))?\s*=\s*')([^']*)('.*)$/i.exec(line);
      if (!m) return escapeHtml(line);
      var varName = m[2];
      var current = values[varName] !== undefined ? values[varName] : m[3];
      var valueHtml = current
        ? '<span class="ph-filled">' + escapeHtml(current) + "</span>"
        : '<span class="ph-token">&lt;' + escapeHtml(varName) + "&gt;</span>";
      return escapeHtml(m[1]) + valueHtml + escapeHtml(m[4]);
    }).join("\n");
  }

  function computeGuards(script, values) {
    var rows = [];
    var text = script.sqlContent;
    var isWrite = WRITE_RE.test(text);

    var tableMatches = text.match(/\b(?:FROM|UPDATE|DELETE FROM|INTO)\s+\[?([\w.]+)\]?/gi) || [];
    var tables = uniq(tableMatches.map(function (m) {
      return m.replace(/^(?:FROM|UPDATE|DELETE FROM|INTO)\s+\[?/i, "").replace(/\]$/, "");
    }));
    if (tables.length) {
      rows.push({ level: "info", text: "สคริปต์นี้อ้างถึง " + tables.length + " ตาราง: " + tables.join(", ") });
    }

    var viewTables = tables.filter(function (t) { return /^vw/i.test(t); });
    if (viewTables.length) {
      rows.push({ level: "danger", text: "ตาราง " + viewTables.join(", ") + " ขึ้นต้นด้วย vw — เป็น View แก้ไขตรงไม่ได้ ต้องหา Base Table จริงก่อนรัน" });
    }

    if (isWrite) {
      var emptyVars = Object.keys(values).filter(function (k) { return !values[k] || !values[k].trim(); });
      if (emptyVars.length) {
        rows.push({ level: "warn", text: "ยังไม่ได้กรอกค่า: " + emptyVars.join(", ") + " — ห้ามคัดลอกจนกว่าจะระบุค่าครบ" });
      } else {
        rows.push({ level: "ok", text: "ค่าตัวแปรครบแล้ว" + (viewTables.length ? "" : " และไม่มีตารางเป็น View") });
      }
    } else {
      rows.push({ level: "ok", text: "สคริปต์นี้เป็นคำสั่งอ่านอย่างเดียว (SELECT) — ปลอดภัย" });
    }

    return { rows: rows, isWrite: isWrite, hasDanger: rows.some(function (r) { return r.level === "danger"; }) };
  }

  var GUARD_ICONS = {
    ok: '<path d="M20 6 9 17l-5-5"></path>',
    warn: '<path d="M12 9v4"></path><path d="M12 17h.01"></path><path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z"></path>',
    danger: '<circle cx="12" cy="12" r="10"></circle><path d="M12 8v4M12 16h.01"></path>',
    info: '<circle cx="12" cy="12" r="10"></circle><path d="M12 16v-4M12 8h.01"></path>'
  };

  function guardListHtml(rows) {
    return '<div class="guard-list">' + rows.map(function (r) {
      return '<div class="guard-row ' + r.level + '"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">' + GUARD_ICONS[r.level] + "</svg><span>" + escapeHtml(r.text) + "</span></div>";
    }).join("") + "</div>";
  }

  // Renders the real grid(s) that came back from POST /api/sql/run - one table per SELECT
  // statement in the script (multi-result-set batches return in source order).
  function sqlResultTablesHtml(tables) {
    if (!tables || !tables.length) return '<div class="sql-run-status">ไม่มีผลลัพธ์</div>';
    return tables.map(function (t, idx) {
      var label = t.label ? escapeHtml(t.label) : "ผลลัพธ์ที่ " + (idx + 1);
      var head = "<tr>" + t.columns.map(function (c) { return "<th>" + escapeHtml(c) + "</th>"; }).join("") + "</tr>";
      var body = t.rows.length
        ? t.rows.map(function (row) {
            return "<tr>" + row.map(function (cell) {
              return "<td>" + (cell === null || cell === undefined ? '<span class="cell-null">NULL</span>' : escapeHtml(cell)) + "</td>";
            }).join("") + "</tr>";
          }).join("")
        : '<tr><td colspan="' + t.columns.length + '" class="cell-empty">ไม่มีแถวข้อมูล</td></tr>';
      return '<div class="sql-result-table"><div class="sql-result-label">' + label + " (" + t.rows.length + " แถว)</div>" +
        '<div class="sql-result-scroll"><table><thead>' + head + "</thead><tbody>" + body + "</tbody></table></div></div>";
    }).join("");
  }

  // Closes the loop after a live "รันจริง" run: the AI never sees what the query actually
  // returned (see console.js history / CLAUDE.md) unless the user explicitly sends it back in -
  // this button formats the result as plain text and drops it into the chat input box (focused,
  // Send enabled) instead of submitting it straight away, so the user can look it over - or edit
  // it - before it actually goes to the AI, the same way anything else they type does.
  function sqlSendToAiRowHtml() {
    return '<div class="sql-send-to-ai-row"><button type="button" class="btn-primary sql-send-to-ai-btn">' +
      '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 2 11 13"></path><path d="M22 2 15 22l-4-9-9-4 20-7Z"></path></svg>' +
      "Send this result to AI</button></div>";
  }

  // Results are only ever sent to the AI when the user presses Send on the chat input (nothing is
  // sent automatically, and the button below only stages it there - it never submits by itself).
  // sqlInfo ({ sql, edited }, optional) is the SQL that produced these rows: it goes into the
  // staged text together with the rows so the AI knows exactly what was run - essential when the
  // person edited the SQL before running it.
  // Appends rather than replaces: the user may run several checks (e.g. one per Known Script card)
  // before sending anything, and wants all of them bundled into one message instead of only the
  // last one staged overwriting the ones before it.
  function bindSqlSendToAiButton(containerEl, tables, sqlInfo) {
    var btn = containerEl.querySelector(".sql-send-to-ai-btn");
    if (!btn) return;
    btn.addEventListener("click", function () {
      if (sqlEditorEl.open) sqlEditorEl.close();
      var addition = formatSqlResultForChat(tables, sqlInfo);
      var existing = chatInputEl.value;
      chatInputEl.value = existing.trim() ? existing.replace(/\s+$/, "") + "\n\n" + addition : addition;
      resizeChatInput();
      if (chatSendBtnEl) chatSendBtnEl.disabled = state.isLoading || !chatInputEl.value.trim();
      setMobileView("left");
      chatInputEl.focus();
      chatInputEl.scrollTop = chatInputEl.scrollHeight;
      showToast("ผลลัพธ์ถูกเพิ่มในช่องพิมพ์แล้ว — ตรวจดูแล้วกด Send");
    });
  }

  // Rows are capped per table so a large check doesn't blow up the next prompt's size - a human
  // skimming the on-screen grid already got the full picture; the AI only needs enough to reason
  // about next steps, not every row.
  function formatSqlResultForChat(tables, sqlInfo) {
    var ranSql = "";
    if (sqlInfo && sqlInfo.sql && sqlInfo.sql.trim()) {
      var text = sqlInfo.sql.trim();
      if (text.length > 2500) text = text.slice(0, 2500) + "\n-- ...(ตัดไว้)";
      ranSql = "SQL ที่รัน" + (sqlInfo.edited ? " (ผู้ใช้แก้ไขเองก่อนรัน ไม่ใช่ตามที่ AI เสนอ)" : "") + ":\n```sql\n" + text + "\n```\n\n";
    }
    if (!tables || !tables.length) {
      return "รันจริงแล้ว ไม่มีผลลัพธ์กลับมา (0 result set)\n\n" + ranSql + "ช่วยบอกว่าควรทำอะไรต่อ";
    }
    var ROW_LIMIT = 20;
    var parts = tables.map(function (t, idx) {
      var label = t.label || ("ผลลัพธ์ที่ " + (idx + 1));
      // A table with no rows carries no information beyond "empty" - printing its full column
      // header anyway is pure noise once a query returns many result sets (e.g. a Known Script
      // like 01.Query_TiresChecker touches ~14 tables and most of them are empty for a given
      // Loading No.), so it collapses to one line instead.
      if (t.rows.length === 0) {
        return label + ": 0 แถว";
      }
      var lines = [label + " (" + t.rows.length + " แถว)", t.columns.join(" | ")];
      t.rows.slice(0, ROW_LIMIT).forEach(function (row) {
        lines.push(row.map(function (cell) { return cell === null || cell === undefined ? "NULL" : String(cell); }).join(" | "));
      });
      if (t.rows.length > ROW_LIMIT) {
        lines.push("...และอีก " + (t.rows.length - ROW_LIMIT) + " แถว (ตัดไว้ ไม่ส่งทั้งหมด)");
      }
      return lines.join("\n");
    });
    return "รันจริงกับฐานข้อมูลแล้ว ได้ผลลัพธ์ดังนี้:\n\n" + ranSql + parts.join("\n\n") + "\n\nช่วยดูผลลัพธ์นี้แล้วบอกว่าควรทำอะไรต่อ";
  }

  // The chat-ready form of one run's outcome: the rendered result tables (or the error text) plus
  // what "Send this result to AI" needs.
  function runOutcomeEntry(result, sqlInfo) {
    if (result && result.success) {
      return { html: sqlResultTablesHtml(result.tables) + sqlSendToAiRowHtml(), tables: result.tables, sqlInfo: sqlInfo };
    }
    var text = (result && result.errorMessage) || "รันสคริปต์ไม่สำเร็จ";
    return { html: '<div class="sql-run-status error">' + escapeHtml(text) + "</div>", errorText: text };
  }

  function runErrorEntry(err) {
    var text = "เชื่อมต่อไม่สำเร็จ: " + err.message;
    return { html: '<div class="sql-run-status error">' + escapeHtml(text) + "</div>", errorText: text };
  }

  async function runScriptLive(script, values) {
    state.scriptRunResults[script.name] = { loading: true, error: null, tables: null };
    renderScriptCard(script);
    try {
      var edit = state.scriptEdits[script.name];
      var result = edit ? await postSqlRun(null, edit.text, {}) : await postSqlRun(script.name, null, values);
      if (!result.success) {
        state.scriptRunResults[script.name] = { loading: false, error: result.errorMessage || "รันสคริปต์ไม่สำเร็จ", tables: null };
      } else {
        state.scriptRunResults[script.name] = {
          loading: false, error: null, tables: result.tables,
          sqlInfo: edit ? { sql: edit.text, edited: true } : { sql: buildScriptSql(script, values), edited: false }
        };
      }
    } catch (err) {
      state.scriptRunResults[script.name] = { loading: false, error: "เชื่อมต่อไม่สำเร็จ: " + err.message, tables: null };
    }
    renderScriptCard(script);
  }

  function copyText(text, label) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(function () {
        showToast("คัดลอก " + label + " แล้ว");
      }).catch(function () {
        showToast("คัดลอกอัตโนมัติไม่ได้ — กรุณาเลือกข้อความและคัดลอกเอง");
      });
    } else {
      showToast("คัดลอกอัตโนมัติไม่ได้ — กรุณาเลือกข้อความและคัดลอกเอง");
    }
  }

  // renderChatStream() rebuilds the whole chat from state.messages every time a message is
  // appended, which would wipe a live-run result grid the moment the next message is added. Results are therefore stored on the message object that owns the SQL block
  // (keyed by the block's position inside it) and re-applied after every render.
  function locateRunSlot(resultEl) {
    var msgEl = resultEl.closest(".msg");
    var idx = Array.prototype.indexOf.call(chatStreamEl.children, msgEl);
    var slots = msgEl.querySelectorAll(".sql-run-results-inline, .ai-sql-run-result");
    return { msg: state.messages[idx], key: Array.prototype.indexOf.call(slots, resultEl) };
  }

  function restoreRunResults() {
    Array.prototype.forEach.call(chatStreamEl.children, function (msgEl, idx) {
      var m = state.messages[idx];
      if (!m || !m.runResults) return;
      var slots = msgEl.querySelectorAll(".sql-run-results-inline, .ai-sql-run-result");
      Object.keys(m.runResults).forEach(function (k) {
        var slotEl = slots[Number(k)];
        var entry = m.runResults[k];
        if (!slotEl || !entry) return;
        slotEl.innerHTML = entry.html;
        if (entry.tables) bindSqlSendToAiButton(slotEl, entry.tables, entry.sqlInfo);
      });
    });
  }

  // A SQL card in the chat (an AI-authored block, or a quoted Known Script) whose SQL was edited in
  // the editor and run shows that edited SQL from then on. The chat is rebuilt from state.messages
  // on every render, so the edit lives on the message (msg.sqlEdits[slotKey] = { text, original,
  // risk }, the same slot numbering as run results) and is re-applied here after each render.
  function restoreSqlEdits() {
    Array.prototype.forEach.call(chatStreamEl.children, function (msgEl, idx) {
      var m = state.messages[idx];
      if (!m || !m.sqlEdits) return;
      var slots = msgEl.querySelectorAll(".sql-run-results-inline, .ai-sql-run-result");
      Object.keys(m.sqlEdits).forEach(function (k) {
        var slotEl = slots[Number(k)];
        if (slotEl) applyEditedSqlToBlock(slotEl, m.sqlEdits[k]);
      });
    });
  }

  function applyEditedSqlToBlock(slotEl, edit) {
    var isAiCard = slotEl.classList.contains("ai-sql-run-result");
    var block = slotEl.closest(isAiCard ? ".ai-sql-block" : ".chat-sql-run-block");
    if (!block) return;
    var codeEl;
    if (isAiCard) {
      codeEl = block.querySelector(".ai-sql-code code");
    } else {
      var pre = block.previousElementSibling;
      codeEl = pre && pre.matches("pre.code-block") ? pre.querySelector("code") : null;
    }
    if (!codeEl) return;
    codeEl.textContent = edit.text;

    var modifies = edit.risk === "modifies";
    var label = (modifies ? "Run live (modifies data)" : "Run live (read-only)") + " — edited SQL";
    var runBtn = block.querySelector(isAiCard ? ".ai-sql-run-btn" : ".chat-sql-run-btn");
    if (!runBtn && isAiCard) {
      // The original SQL was blocked, so the card had no Run button - the edited SQL can be run.
      runBtn = document.createElement("button");
      runBtn.type = "button";
      var row = block.querySelector(".ai-sql-copy-row");
      if (row) row.insertBefore(runBtn, row.firstChild);
    }
    if (runBtn) {
      runBtn.disabled = false;
      runBtn.removeAttribute("title");
      runBtn.setAttribute("data-edited", "true");
      runBtn.setAttribute("data-write", modifies ? "true" : "false");
      runBtn.className = isAiCard
        ? "sql-run-btn ai-sql-run-btn" + (modifies ? " ai-sql-run-btn-write" : "")
        : "sql-run-btn chat-sql-run-btn" + (modifies ? " chat-sql-run-btn-write" : "");
      runBtn.textContent = label;
    }
    Array.prototype.forEach.call(block.querySelectorAll(".ai-sql-unfilled-hint"), function (h) { h.remove(); });

    if (!block.querySelector(".sql-edited-note")) {
      var note = document.createElement("div");
      note.className = "sql-edited-note";
      note.textContent = "SQL ที่แก้ไขล่าสุด (แก้ไขแล้ว ไม่ใช่ต้นฉบับ) — ปุ่มรันจะใช้ SQL นี้";
      var anchor = isAiCard ? block.querySelector(".ai-sql-code") : null;
      if (anchor) anchor.parentNode.insertBefore(note, anchor);
      else block.insertBefore(note, block.firstChild);
    }
  }

  async function finishInlineRun(slot, isWrite, doRun, sqlInfo) {
    var entry;
    try {
      var result = await doRun();
      if (result.success) {
        entry = {
          html: sqlResultTablesHtml(result.tables) + sqlSendToAiRowHtml(),
          tables: result.tables,
          sqlInfo: sqlInfo
        };
      } else {
        entry = { html: '<div class="sql-run-status error">' + escapeHtml(result.errorMessage || "รันสคริปต์ไม่สำเร็จ") + "</div>" };
      }
    } catch (err) {
      entry = { html: '<div class="sql-run-status error">เชื่อมต่อไม่สำเร็จ: ' + escapeHtml(err.message) + "</div>" };
    }

    if (slot.msg) {
      slot.msg.runResults = slot.msg.runResults || {};
      slot.msg.runResults[slot.key] = entry;
      restoreRunResults();
    }
  }

  // Write execution (DELETE/UPDATE/INSERT) is real and unrecoverable - the user explicitly chose
  // to allow it, but every write click still gets one extra native confirm() here on top of
  // SqlScriptRunner re-checking SqlRun:AllowWrites and wrapping the execution in a real DB
  // transaction server-side. Returns false (caller should abort) if the user cancels.
  function confirmDestructiveRun() {
    return window.confirm("คำสั่งนี้จะแก้ไขข้อมูลจริงในฐานข้อมูล (DELETE/UPDATE/INSERT) — แก้คืนไม่ได้\n\nยืนยันว่าตรวจสอบค่าที่กรอกแล้ว และต้องการรันจริงหรือไม่?");
  }

  // A plain code block in the chat answer that RunnableSqlFormatter.cs (Web project) matched to
  // a real Known Script gets this button attached right after it server-side (see
  // "chat-sql-run-block" in that formatter). Runs the exact same /Index?handler=RunSql endpoint
  // and result-table renderer SQL Studio's cards already use - just triggered from inline chat
  // text instead of the SQL Studio tab, and reading its placeholder values from the code block's
  // own displayed text (the AI already fills DECLARE lines with real values when it quotes the
  // script) instead of from the SQL Studio card's input fields.
  async function handleChatSqlRun(btn) {
    var scriptName = btn.getAttribute("data-script-name");
    var block = btn.closest(".chat-sql-run-block");
    var pre = block && block.previousElementSibling;
    var codeEl = pre && pre.matches("pre.code-block") ? pre.querySelector("code") : null;
    var resultEl = block && block.querySelector(".sql-run-results-inline");
    if (!scriptName || !codeEl || !resultEl) return;

    if (btn.getAttribute("data-write") === "true" && !confirmDestructiveRun()) return;

    var values = {};
    detectPlaceholders(codeEl.textContent).forEach(function (p) { values[p.name] = p.defaultValue; });

    var originalLabel = btn.textContent;
    btn.disabled = true;
    btn.textContent = "Running...";
    var slot = locateRunSlot(resultEl);
    resultEl.innerHTML = '<div class="sql-run-status loading">กำลังเชื่อมต่อฐานข้อมูลจริงและรันคำสั่ง...</div>';

    var editedBlock = btn.getAttribute("data-edited") === "true";
    var sqlNow = codeEl.textContent;
    await finishInlineRun(slot, btn.getAttribute("data-write") === "true", function () {
      return editedBlock ? postSqlRun(null, sqlNow, {}) : postSqlRun(scriptName, null, values);
    }, { sql: sqlNow, edited: editedBlock });

    btn.disabled = false;
    btn.textContent = originalLabel;
  }

  // Runs the raw SQL text inside an ai-sql-block directly (no known script name - the user
  // explicitly chose to allow running AI-authored SQL, including write statements now, still
  // gated by a native confirm() here and SqlScriptRunner re-checking AllowWrites server-side
  // regardless).
  async function handleAiSqlRun(btn) {
    var block = btn.closest(".ai-sql-block");
    var codeEl = block && block.querySelector(".ai-sql-code code");
    var resultEl = block && block.querySelector(".ai-sql-run-result");
    if (!codeEl || !resultEl) return;

    if (btn.classList.contains("ai-sql-run-btn-write") && !confirmDestructiveRun()) return;

    var values = {};
    detectPlaceholders(codeEl.textContent).forEach(function (p) { values[p.name] = p.defaultValue; });

    var originalLabel = btn.textContent;
    btn.disabled = true;
    btn.textContent = "Running...";
    var slot = locateRunSlot(resultEl);
    var sqlText = codeEl.textContent;
    resultEl.innerHTML = '<div class="sql-run-status loading">กำลังเชื่อมต่อฐานข้อมูลจริงและรันคำสั่ง...</div>';

    await finishInlineRun(slot, btn.classList.contains("ai-sql-run-btn-write"), function () {
      return postSqlRun(null, sqlText, values);
    }, { sql: sqlText, edited: false });

    btn.disabled = false;
    btn.textContent = originalLabel;
  }

  // AI-suggested SQL blocks (see SimpleMarkdown.cs's ai-sql-block wrapper) and the "รันจริง"
  // buttons RunnableSqlFormatter.cs attaches to matched real scripts both render inline inside
  // chat answers, which are appended dynamically - a delegated listener on the whole stream
  // covers every one of them without needing to re-bind after each appendMessage call.
  chatStreamEl.addEventListener("click", function (e) {
    var chatRunBtn = e.target.closest(".chat-sql-run-btn");
    if (chatRunBtn) {
      if (!chatRunBtn.disabled) handleChatSqlRun(chatRunBtn);
      return;
    }

    var aiRunBtn = e.target.closest(".ai-sql-run-btn");
    if (aiRunBtn) {
      if (!aiRunBtn.disabled) handleAiSqlRun(aiRunBtn);
      return;
    }

    var aiEditBtn = e.target.closest(".ai-sql-edit-btn");
    if (aiEditBtn) {
      var aiBlock = aiEditBtn.closest(".ai-sql-block");
      var aiCode = aiBlock && aiBlock.querySelector(".ai-sql-code code");
      var aiResultEl = aiBlock && aiBlock.querySelector(".ai-sql-run-result");
      if (aiCode && aiResultEl) {
        var aiSlot = locateRunSlot(aiResultEl);
        var aiPrior = aiSlot.msg && aiSlot.msg.sqlEdits && aiSlot.msg.sqlEdits[aiSlot.key];
        openSqlEditor("SQL ที่ AI แต่งขึ้นเอง", aiCode.textContent, { kind: "chat", slot: aiSlot }, aiPrior ? aiPrior.original : null);
      }
      return;
    }

    var chatEditBtn = e.target.closest(".chat-sql-edit-btn");
    if (chatEditBtn) {
      var runBlock = chatEditBtn.closest(".chat-sql-run-block");
      var quotedPre = runBlock && runBlock.previousElementSibling;
      var quotedCode = quotedPre && quotedPre.matches("pre.code-block") ? quotedPre.querySelector("code") : null;
      var quotedResultEl = runBlock && runBlock.querySelector(".sql-run-results-inline");
      if (quotedCode && quotedResultEl) {
        var quotedSlot = locateRunSlot(quotedResultEl);
        var quotedPrior = quotedSlot.msg && quotedSlot.msg.sqlEdits && quotedSlot.msg.sqlEdits[quotedSlot.key];
        openSqlEditor("สคริปต์ " + chatEditBtn.getAttribute("data-script-name") + " (ตามที่ AI อ้างในคำตอบ)", quotedCode.textContent, { kind: "chat", slot: quotedSlot }, quotedPrior ? quotedPrior.original : null);
      }
      return;
    }

    var copyBtn = e.target.closest(".ai-sql-copy-btn");
    if (!copyBtn) return;
    var block = copyBtn.closest(".ai-sql-block");
    var code = block && block.querySelector(".ai-sql-code code");
    if (!code) return;
    copyText(code.textContent, "SQL ที่ AI แต่งขึ้นเอง");
  });

  function renderSqlStudio() {
    if (!state.relevantScripts.length) {
      rightContentEl.innerHTML =
        '<div class="right-empty">ยังไม่มีสคริปต์ที่เกี่ยวข้องกับคำตอบล่าสุด — ระบบจับคู่จากชื่อสคริปต์ใน Known Scripts เทียบกับเนื้อเคส/คำถาม (ครอบคลุมกลุ่ม Reset Tirechecker เป็นหลักในตอนนี้)</div>';
      return;
    }

    rightContentEl.innerHTML = state.relevantScripts.map(function (s) {
      return '<div class="script-card" data-script="' + escapeHtml(s.name) + '"></div>';
    }).join("");

    state.relevantScripts.forEach(function (script) { renderScriptCard(script); });
  }

  function renderScriptCard(script) {
    var card = rightContentEl.querySelector('.script-card[data-script="' + CSS.escape(script.name) + '"]');
    if (!card) return;

    var values = ensurePlaceholderState(script);
    var placeholders = detectPlaceholders(script.sqlContent);
    var guards = computeGuards(script, values);
    var edit = state.scriptEdits[script.name] || null; // set once SQL edited in the editor has been run
    var isWriteCard = edit ? edit.risk === "modifies" : guards.isWrite;
    var canCopy = guards.isWrite ? (!guards.hasDanger && placeholders.every(function (p) { return values[p.name] && values[p.name].trim(); })) : !guards.hasDanger;

    var fieldsHtml = !edit && placeholders.length
      ? '<div class="placeholder-fields">' + placeholders.map(function (p) {
          return '<div class="ph-field"><label for="ph-' + escapeHtml(script.name) + "-" + escapeHtml(p.name) + '">' + escapeHtml(p.name) + '</label>' +
            '<input type="text" class="mono" data-ph="' + escapeHtml(p.name) + '" id="ph-' + escapeHtml(script.name) + "-" + escapeHtml(p.name) + '" value="' + escapeHtml(values[p.name] || "") + '"></div>';
        }).join("") + "</div>"
      : "";

    // Advisory only (no checkbox gate any more): the red button plus the confirm() dialog in
    // runScriptLive are the actual safeguards; this just reminds the user before they click.
    var verifyGateHtml = isWriteCard
      ? '<div class="verify-gate verify-notice">สคริปต์นี้แก้ไขข้อมูลจริง ตรวจสอบค่าที่กรอกให้ดีก่อนกดรัน — แก้คืนไม่ได้</div>'
      : "";

    // Write scripts need all placeholders filled (canCopy), on top of the confirm() dialog
    // runScriptLive shows for a write script. The
    // read-only button's own enable/disable behavior is unchanged from before (loading-only).
    var run = state.scriptRunResults[script.name];
    var allFilled = edit ? true : placeholders.every(function (p) { return values[p.name] && values[p.name].trim(); });
    var canRun = edit ? true : ((guards.isWrite ? canCopy : true) && allFilled);
    var runBtnHtml = '<button type="button" class="sql-run-btn' + (isWriteCard ? " sql-run-btn-write" : "") + '" ' +
        (!canRun || (run && run.loading) ? "disabled" : "") + '>' +
        '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 3v18l15-9L5 3Z"></path></svg>' +
        (run && run.loading ? "Running..." : (isWriteCard ? "Run live (modifies data)" : "Run live (read-only)")) +
      "</button>" +
      (edit ? '<button type="button" class="sql-edit-secondary sql-edit-reset">Reset to original script</button>' : "");

    var runResultHtml = "";
    if (run) {
      if (run.loading) {
        runResultHtml = '<div class="sql-run-status loading">กำลังเชื่อมต่อฐานข้อมูลจริงและรันคำสั่ง...</div>';
      } else if (run.error) {
        runResultHtml = '<div class="sql-run-status error">' + escapeHtml(run.error) + "</div>";
      } else {
        runResultHtml = '<div class="sql-run-results">' + sqlResultTablesHtml(run.tables) + sqlSendToAiRowHtml() + "</div>";
      }
    }

    card.innerHTML =
      '<div class="script-card-head"><h3>' + escapeHtml(script.name) + "</h3><p>" + escapeHtml(script.description) + "</p>" +
      '<span class="script-kind ' + (isWriteCard ? "write" : "read") + '">' + (isWriteCard ? "แก้ไขข้อมูล" : "อ่านอย่างเดียว") + "</span></div>" +
      fieldsHtml +
      (edit
        ? '<div class="sql-edited-note">SQL ที่แก้ไขล่าสุด (แก้ไขแล้ว ไม่ใช่สคริปต์ต้นฉบับ) — ปุ่มรันจะใช้ SQL นี้</div><pre class="sql-block-body">' + escapeHtml(edit.text) + "</pre>"
        : '<pre class="sql-block-body">' + renderScriptText(script, values) + "</pre>") +
      verifyGateHtml +
      (edit ? "" : '<div class="guard-heading">Static Guard Check</div>' + guardListHtml(guards.rows)) +
      '<div class="sql-copy-row">' + (allFilled ? "" : '<span class="ai-sql-unfilled-hint">กรอกค่าให้ครบก่อน จึงจะรันจริงได้</span>') + runBtnHtml + '<button type="button" class="sql-edit-secondary sql-edit-btn">Edit</button><button type="button" class="sql-copy-btn" ' + (canCopy ? "" : "disabled") + '><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="9" y="9" width="11" height="11" rx="1.5"></rect><path d="M5 15V5a1.5 1.5 0 0 1 1.5-1.5H15"></path></svg>Copy</button></div>' +
      runResultHtml;

    Array.prototype.forEach.call(card.querySelectorAll("[data-ph]"), function (input) {
      input.addEventListener("input", function () {
        values[input.getAttribute("data-ph")] = input.value;
        var caret = input.selectionStart;
        renderScriptCard(script);
        var again = document.getElementById(input.id);
        if (again) { again.focus(); again.setSelectionRange(caret, caret); }
      });
    });


    var runBtn = card.querySelector(".sql-run-btn");
    if (runBtn) runBtn.addEventListener("click", function () {
      if (runBtn.disabled) return;
      if (isWriteCard && !confirmDestructiveRun()) return;
      runScriptLive(script, values);
    });

    if (run && !run.loading && !run.error) {
      bindSqlSendToAiButton(card, run.tables, run.sqlInfo);
    }

    var copyBtn = card.querySelector(".sql-copy-btn");
    if (copyBtn) copyBtn.addEventListener("click", function () {
      if (copyBtn.disabled) return;
      copyText(edit ? edit.text : buildScriptSql(script, values), script.name);
    });

    var editBtn = card.querySelector(".sql-edit-btn");
    if (editBtn) editBtn.addEventListener("click", function () {
      openSqlEditor("SQL Studio — " + script.name, edit ? edit.text : buildScriptSql(script, values), { kind: "studio", script: script }, edit ? edit.original : null);
    });

    var resetBtn = card.querySelector(".sql-edit-reset");
    if (resetBtn) resetBtn.addEventListener("click", function () {
      delete state.scriptEdits[script.name];
      delete state.scriptRunResults[script.name];
      renderScriptCard(script);
    });
  }

  // The script's SQL as plain text with the values currently typed into its fields written into the
  // DECLARE lines (and the hardcoded [MoCS]. qualifier dropped) - what Copy puts on the clipboard
  // and what the SQL editor opens with.
  function buildScriptSql(script, values) {
    return stripMocsQualifier(script.sqlContent).replace(/\r\n/g, "\n").split("\n").map(function (line) {
      var m = /^(\s*DECLARE\s+@(\w+)\s+\w+(?:\([^)]*\))?\s*=\s*')([^']*)('.*)$/i.exec(line);
      if (!m) return line;
      var current = values[m[2]] !== undefined ? values[m[2]] : m[3];
      return m[1] + current + m[4];
    }).join("\n");
  }

  /* =========================== SQL editor =========================== */

  // A dialog for changing a SQL card's text before running it. The chat is rebuilt from
  // state.messages on every render, so an editor inside a card would lose what was typed the moment
  // anything else happened; a dialog keeps its own state outside the chat. What it runs goes through
  // the same /Index?handler=RunSql -> API path as every other run, and the API re-checks everything
  // itself (blocked statements, the writes setting, unfilled values). The notice shown here while
  // typing comes from the same shared rule (/Index?handler=ClassifySql), so it never disagrees.
  var sqlEditorEl = document.getElementById("sqlEditor");
  var sqlEditorTextEl = document.getElementById("sqlEditorText");
  var sqlEditorNoticeEl = document.getElementById("sqlEditorNotice");
  var sqlEditorRunEl = document.getElementById("sqlEditorRun");
  var sqlEditorResultsEl = document.getElementById("sqlEditorResults");
  var sqlEditorOriginEl = document.getElementById("sqlEditorOrigin");
  // last = the newest classification received; pending = the text changed since then (Run stays off
  // until the answer for the CURRENT text is in, so the confirm() decision is never made on a stale one).
  var sqlEditor = { original: "", target: null, last: null, pending: false, running: false, seq: 0, timer: null };

  // target = the card whose result slot a run from the editor overwrites:
  //   { kind: "chat",   slot: { msg, key } }  a SQL card inside a chat message (same slot bookkeeping
  //                                          as inline runs, so it survives chat re-renders)
  //   { kind: "studio", script }              a SQL Studio card
  function showEditorRunOnCard(entry, sql) {
    var t = sqlEditor.target;
    if (!t) return;
    // Running the original text again (Reset) clears the edit; anything else becomes the card's SQL.
    var unchanged = sql.trim() === sqlEditor.original.trim();
    var edit = unchanged ? null : { text: sql, original: sqlEditor.original, risk: sqlEditor.last ? sqlEditor.last.risk : "readOnly" };
    if (t.kind === "chat") {
      if (t.slot && t.slot.msg && state.messages.indexOf(t.slot.msg) !== -1) {
        var msg = t.slot.msg;
        msg.runResults = msg.runResults || {};
        msg.runResults[t.slot.key] = entry;
        msg.sqlEdits = msg.sqlEdits || {};
        if (edit) msg.sqlEdits[t.slot.key] = edit; else delete msg.sqlEdits[t.slot.key];
        var scrollTop = chatStreamEl.scrollTop;
        renderChatStream(true);
        chatStreamEl.scrollTop = scrollTop;
      }
    } else if (t.kind === "studio") {
      if (edit) state.scriptEdits[t.script.name] = edit; else delete state.scriptEdits[t.script.name];
      state.scriptRunResults[t.script.name] = entry.tables
        ? { loading: false, error: null, tables: entry.tables, sqlInfo: entry.sqlInfo }
        : { loading: false, error: entry.errorText || "รันสคริปต์ไม่สำเร็จ", tables: null };
      renderScriptCard(t.script);
    }
  }

  function openSqlEditor(origin, sql, target, originalSql) {
    sqlEditor.target = target || null;
    sqlEditor.original = originalSql || sql;
    sqlEditorOriginEl.textContent = origin;
    sqlEditorTextEl.value = sql;
    sqlEditorResultsEl.innerHTML = "";
    if (!sqlEditorEl.open) sqlEditorEl.showModal();
    scheduleEditorClassify(0);
    sqlEditorTextEl.focus();
    // focus() leaves the caret at the end and scrolls there - start at the top of the SQL instead.
    sqlEditorTextEl.setSelectionRange(0, 0);
    sqlEditorTextEl.scrollTop = 0;
  }

  function scheduleEditorClassify(delay) {
    sqlEditor.pending = true;
    syncEditorControls();
    clearTimeout(sqlEditor.timer);
    var seq = ++sqlEditor.seq;
    sqlEditor.timer = setTimeout(async function () {
      try {
        var res = await fetch("/Index?handler=ClassifySql", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ sqlText: sqlEditorTextEl.value })
        });
        if (!res.ok) throw new Error("HTTP " + res.status);
        var classification = await res.json();
        if (seq !== sqlEditor.seq) return; // superseded by a newer edit
        sqlEditor.last = classification;
        sqlEditor.pending = false;
        syncEditorControls();
      } catch (err) {
        if (seq !== sqlEditor.seq) return;
        sqlEditorNoticeEl.textContent = "ตรวจ SQL ไม่สำเร็จ: " + err.message + " — ยังรันไม่ได้";
      }
    }, delay);
  }

  function syncEditorControls() {
    var c = sqlEditor.pending ? null : sqlEditor.last;
    var empty = !sqlEditorTextEl.value.trim();
    var blocked = !!c && c.risk === "blocked";
    var modifies = !!c && c.risk === "modifies";
    var unfilled = !!c && c.unfilled;

    sqlEditorRunEl.disabled = !c || empty || blocked || unfilled || sqlEditor.running;
    sqlEditorRunEl.className = "sql-run-btn" + (modifies ? " sql-run-btn-write" : "");
    sqlEditorRunEl.textContent = sqlEditor.running ? "Running..." : (modifies ? "Run live (modifies data)" : "Run live (read-only)");

    if (!c) sqlEditorNoticeEl.textContent = "กำลังตรวจ SQL…";
    else if (empty) sqlEditorNoticeEl.textContent = "ยังไม่มี SQL";
    else if (blocked) sqlEditorNoticeEl.textContent = c.reason;
    else if (unfilled) sqlEditorNoticeEl.textContent = "ยังมีค่าที่ต้องระบุ (<...> หรือค่าว่าง) — แก้ให้ครบก่อนจึงจะรันจริงได้";
    else if (modifies) sqlEditorNoticeEl.textContent = "SQL นี้แก้ไขข้อมูลจริง ตรวจสอบให้ดีก่อนกดรัน — แก้คืนไม่ได้";
    else sqlEditorNoticeEl.textContent = "อ่านอย่างเดียว — รันได้เลย";
  }

  sqlEditorTextEl.addEventListener("input", function () { scheduleEditorClassify(300); });

  sqlEditorRunEl.addEventListener("click", async function () {
    if (sqlEditorRunEl.disabled) return;
    var sql = sqlEditorTextEl.value;
    if (sqlEditor.last && sqlEditor.last.risk === "modifies" && !confirmDestructiveRun()) return;

    sqlEditor.running = true;
    syncEditorControls();
    sqlEditorResultsEl.innerHTML = '<div class="sql-run-status loading">กำลังเชื่อมต่อฐานข้อมูลจริงและรันคำสั่ง...</div>';
    var entry;
    try {
      var result = await postSqlRun(null, sql, {});
      entry = runOutcomeEntry(result, { sql: sql, edited: sql.trim() !== sqlEditor.original.trim() });
    } catch (err) {
      entry = runErrorEntry(err);
    }

    // Shown in the dialog, and written over the previous result of the card the editor was opened
    // from (no new chat message - the card simply shows the newest result).
    sqlEditorResultsEl.innerHTML = entry.html;
    if (entry.tables) bindSqlSendToAiButton(sqlEditorResultsEl, entry.tables, entry.sqlInfo);
    showEditorRunOnCard(entry, sql);
    sqlEditor.running = false;
    syncEditorControls();
  });

  document.getElementById("sqlEditorCopy").addEventListener("click", function () {
    copyText(sqlEditorTextEl.value, "SQL");
  });

  document.getElementById("sqlEditorReset").addEventListener("click", function () {
    sqlEditorTextEl.value = sqlEditor.original;
    scheduleEditorClassify(0);
  });

  document.getElementById("sqlEditorClose").addEventListener("click", function () { sqlEditorEl.close(); });

  // Clicking the dimmed area outside the dialog closes it. A click on the backdrop reports the
  // dialog itself as its target, and so does a click on the dialog's own padding - hence also
  // comparing against its rectangle. Only clicks TARGETED at the dialog count: pressing Enter/Space on
  // a focused button also produces a click event (with coordinates 0,0 - "outside"), and that must
  // never close it.
  sqlEditorEl.addEventListener("click", function (e) {
    if (e.target !== sqlEditorEl) return;
    var r = sqlEditorEl.getBoundingClientRect();
    var inside = e.clientX >= r.left && e.clientX <= r.right && e.clientY >= r.top && e.clientY <= r.bottom;
    if (!inside) sqlEditorEl.close();
  });

  /* =========================== Save-case editor =========================== */

  var saveCaseEditorEl = document.getElementById("saveCaseEditor");
  var saveCaseEditorTextEl = document.getElementById("saveCaseEditorText");
  var saveCaseEditorNoticeEl = document.getElementById("saveCaseEditorNotice");
  var saveCaseEditorSaveEl = document.getElementById("saveCaseEditorSave");
  // Remembers which chat message triggered the dialog, so Save knows which conversationId/case
  // ids/script names to submit alongside whatever text the user ends up with after editing.
  var saveCaseState = { msg: null, saving: false };

  function openSaveCaseEditor(msg) {
    saveCaseState.msg = msg;
    saveCaseEditorTextEl.value = msg.rawText || "";
    saveCaseEditorNoticeEl.textContent = "";
    if (!saveCaseEditorEl.open) saveCaseEditorEl.showModal();
    syncSaveCaseControls();
    saveCaseEditorTextEl.focus();
    saveCaseEditorTextEl.setSelectionRange(0, 0);
    saveCaseEditorTextEl.scrollTop = 0;
  }

  function syncSaveCaseControls() {
    saveCaseEditorSaveEl.disabled = saveCaseState.saving || !saveCaseEditorTextEl.value.trim();
    saveCaseEditorSaveEl.textContent = saveCaseState.saving ? "กำลังบันทึก..." : "บันทึก";
  }

  saveCaseEditorTextEl.addEventListener("input", syncSaveCaseControls);

  saveCaseEditorSaveEl.addEventListener("click", async function () {
    if (saveCaseEditorSaveEl.disabled) return;
    var msg = saveCaseState.msg;
    var info = msg && msg.saveCaseInfo;
    var summary = saveCaseEditorTextEl.value.trim();
    if (!info || !summary) return;

    saveCaseState.saving = true;
    syncSaveCaseControls();
    saveCaseEditorNoticeEl.textContent = "กำลังบันทึก...";
    try {
      var res = await fetch("/Index?handler=SaveCase", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          conversationId: info.conversationId,
          summary: summary,
          referencedCaseIds: info.referencedCaseIds,
          knownScripts: info.knownScripts
        })
      });
      if (!res.ok) throw new Error("HTTP " + res.status);
      await res.json();
      saveCaseEditorEl.close();
      showToast('บันทึกเคสแล้ว — ดูได้ที่หน้า "เคสที่บันทึกแล้ว"');
    } catch (err) {
      saveCaseEditorNoticeEl.textContent = "บันทึกไม่สำเร็จ: " + err.message;
    } finally {
      saveCaseState.saving = false;
      syncSaveCaseControls();
    }
  });

  document.getElementById("saveCaseEditorCancel").addEventListener("click", function () { saveCaseEditorEl.close(); });
  document.getElementById("saveCaseEditorClose").addEventListener("click", function () { saveCaseEditorEl.close(); });

  saveCaseEditorEl.addEventListener("click", function (e) {
    if (e.target !== saveCaseEditorEl) return;
    var r = saveCaseEditorEl.getBoundingClientRect();
    var inside = e.clientX >= r.left && e.clientX <= r.right && e.clientY >= r.top && e.clientY <= r.bottom;
    if (!inside) saveCaseEditorEl.close();
  });

  /* =========================== Mobile panel switch =========================== */

  function setMobileView(view) {
    state.mobileView = view;
    workspaceEl.setAttribute("data-mobile", view);
    document.getElementById("switchLeftBtn").setAttribute("aria-selected", view === "left");
    document.getElementById("switchRightBtn").setAttribute("aria-selected", view === "right");
  }

  document.getElementById("switchLeftBtn").addEventListener("click", function () { setMobileView("left"); });
  document.getElementById("switchRightBtn").addEventListener("click", function () { setMobileView("right"); });

  /* =========================== Right panel collapse (desktop) =========================== */

  var panelCollapseBtnEl = document.getElementById("panelCollapseBtn");
  panelCollapseBtnEl.addEventListener("click", function () {
    setRightCollapsed(!isRightCollapsed());
  });

  /* =========================== Panel resize =========================== */

  // Dragging the boundary sets --left-w (the chat column width in px) on the workspace grid;
  // double-click clears it back to the default proportion. The width is remembered per browser.
  var panelResizerEl = document.getElementById("panelResizer");
  var LEFT_W_KEY = "maAssistant.leftPanelWidth";
  var MIN_LEFT_PX = 340;
  var MIN_RIGHT_PX = 320;

  function clampLeftWidth(px) {
    var total = workspaceEl.getBoundingClientRect().width;
    return Math.max(MIN_LEFT_PX, Math.min(px, total - MIN_RIGHT_PX));
  }

  function applyLeftWidth(px) {
    if (px == null) workspaceEl.style.removeProperty("--left-w");
    else workspaceEl.style.setProperty("--left-w", clampLeftWidth(px) + "px");
  }

  try {
    var savedLeftW = parseFloat(localStorage.getItem(LEFT_W_KEY));
    if (savedLeftW) applyLeftWidth(savedLeftW);
  } catch (e) { /* storage unavailable - default width */ }

  panelResizerEl.addEventListener("pointerdown", function (e) {
    e.preventDefault();
    panelResizerEl.setPointerCapture(e.pointerId);
    workspaceEl.classList.add("is-resizing");
    var left = workspaceEl.getBoundingClientRect().left;

    function onMove(ev) { applyLeftWidth(ev.clientX - left); }
    function onUp() {
      panelResizerEl.removeEventListener("pointermove", onMove);
      panelResizerEl.removeEventListener("pointerup", onUp);
      panelResizerEl.removeEventListener("pointercancel", onUp);
      workspaceEl.classList.remove("is-resizing");
      try { localStorage.setItem(LEFT_W_KEY, String(parseFloat(workspaceEl.style.getPropertyValue("--left-w")) || "")); } catch (e) { /* ignore */ }
    }
    panelResizerEl.addEventListener("pointermove", onMove);
    panelResizerEl.addEventListener("pointerup", onUp);
    panelResizerEl.addEventListener("pointercancel", onUp);
  });

  panelResizerEl.addEventListener("dblclick", function () {
    applyLeftWidth(null);
    try { localStorage.removeItem(LEFT_W_KEY); } catch (e) { /* ignore */ }
  });

  // A saved width can become too wide after the window shrinks - re-clamp so the right panel never vanishes.
  window.addEventListener("resize", function () {
    var w = parseFloat(workspaceEl.style.getPropertyValue("--left-w"));
    if (w) applyLeftWidth(w);
  });

  /* =========================== Session persistence =========================== */

  // Everything that makes up "the current session" (mirrors exactly what the New question
  // handler below resets) survives navigating away and back - e.g. clicking History and using the
  // browser back button, or just reloading - by round-tripping through sessionStorage (per-tab,
  // cleared when the tab closes; unlike localStorage it doesn't bleed into a different tab/window
  // the user opens fresh). Every message object in state.messages is already a plain
  // {role, html, ...} record (see appendMessage) with any live SQL-run results/edits attached
  // directly on it (m.runResults / m.sqlEdits - see restoreRunResults/restoreSqlEdits), so nothing
  // needs special handling to serialize: state.messages round-trips whole. Only a real "New
  // question" click (or a fresh /Index visit with no saved session) should ever clear this - a page
  // navigation by itself must not look like the user asked to start over.
  var SESSION_KEY = "maAssistant.session";
  var PERSISTED_KEYS = [
    "conversationId", "messages", "caseCache", "lastSearchCases", "relevantScripts",
    "activeCaseId", "rightTab", "scriptPlaceholders", "scriptRunResults", "scriptEdits",
    "hasShownSearchPreview"
  ];

  function persistSession() {
    try {
      var snapshot = {};
      PERSISTED_KEYS.forEach(function (k) { snapshot[k] = state[k]; });
      sessionStorage.setItem(SESSION_KEY, JSON.stringify(snapshot));
    } catch (e) { /* storage unavailable/full - session just won't survive navigation this time */ }
  }

  function clearPersistedSession() {
    try { sessionStorage.removeItem(SESSION_KEY); } catch (e) { /* ignore */ }
  }

  function restorePersistedSession() {
    try {
      var raw = sessionStorage.getItem(SESSION_KEY);
      if (!raw) return;
      var saved = JSON.parse(raw);
      PERSISTED_KEYS.forEach(function (k) {
        if (Object.prototype.hasOwnProperty.call(saved, k)) state[k] = saved[k];
      });
    } catch (e) { /* corrupt/unavailable - fall back to a blank session */ }
  }

  /* =========================== New session =========================== */

  // A new session just means a fresh conversationId (the API mints one when it receives null, so
  // no last-turn context or follow-up shortcut state carries over) plus a clean UI. The button is
  // disabled while a request is in flight so a late response can never repopulate the old state.
  newChatBtnEl.addEventListener("click", function () {
    if (state.isLoading) return;
    state.conversationId = null;
    state.messages = [];
    state.caseCache = {};
    state.lastSearchCases = [];
    state.relevantScripts = [];
    state.activeCaseId = null;
    state.scriptPlaceholders = {};
    state.scriptRunResults = {};
    state.scriptEdits = {};
    state.hasShownSearchPreview = false;
    state.rightFresh = false;
    clearPersistedSession();
    closeCaseDrawer();
    chatInputEl.value = "";
    resizeChatInput();
    if (chatSendBtnEl) chatSendBtnEl.disabled = true;
    renderChatStream();
    renderRightTabs();
    renderRightContent();
    setMobileView("left");
    chatInputEl.focus();
    showToast("เริ่มคำถามใหม่แล้ว");
  });

  /* =========================== Init =========================== */

  restorePersistedSession();
  renderChatStream();
  renderQuickCommandBar();
  renderRightTabs();
  renderRightContent();
})();
