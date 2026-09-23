using AIforMAsupport.Api.Services.Ai;
using AIforMAsupport.Api.Services.Conversation;
using AIforMAsupport.Api.Services.History;
using AIforMAsupport.Api.Services.KnowledgeBase;
using AIforMAsupport.Api.Services.KnownScripts;
using AIforMAsupport.Api.Services.SavedCases;
using AIforMAsupport.Api.Services.Sql;

// Without this, Thai log lines (questions, case summaries) render as "?" on Windows consoles
// whose active code page isn't UTF-8 - the underlying data is fine, only the console output is
// affected, but it makes the logs useless for debugging Thai input.
Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// IAiClient is the only thing that changes when this swaps from the headless claude CLI to a
// direct Anthropic API key later (see Services/Ai) - everything else in the app talks to the
// interface only, never to ClaudeCodeHeadlessAiClient directly.
builder.Services.Configure<ClaudeCliOptions>(builder.Configuration.GetSection(ClaudeCliOptions.SectionName));
builder.Services.AddSingleton<IAiClient, ClaudeCodeHeadlessAiClient>();

// HistoryOptions is configured once, up here, because it's now shared by three stores
// (ConversationHistory, SavedCases, and KbAdditions below) that all live in the same SQLite file.
builder.Services.Configure<HistoryOptions>(builder.Configuration.GetSection(HistoryOptions.SectionName));

builder.Services.Configure<KbDataOptions>(builder.Configuration.GetSection(KbDataOptions.SectionName));
builder.Services.AddSingleton<CsvKbCaseRepository>();
builder.Services.AddSingleton<IKbAdditionStore, SqliteKbAdditionStore>();
builder.Services.AddSingleton<IKbCaseRepository, CompositeKbCaseRepository>();
builder.Services.AddSingleton<IKbContextProvider, KeywordSearchKbContextProvider>();
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();

builder.Services.Configure<KnownScriptsOptions>(builder.Configuration.GetSection(KnownScriptsOptions.SectionName));
builder.Services.AddSingleton<IKnownScriptRepository, FileKnownScriptRepository>();

builder.Services.Configure<SqlRunOptions>(builder.Configuration.GetSection(SqlRunOptions.SectionName));
builder.Services.AddSingleton<ISqlScriptRunner, SqlScriptRunner>();

builder.Services.Configure<DbSchemaOptions>(builder.Configuration.GetSection(DbSchemaOptions.SectionName));
builder.Services.AddSingleton<IDbSchemaProvider, FileDbSchemaProvider>();

builder.Services.AddSingleton<IConversationHistoryStore, SqliteConversationHistoryStore>();
// Shares HistoryOptions (same SQLite file, different table) - see SqliteSavedCaseStore.
builder.Services.AddSingleton<ISavedCaseStore, SqliteSavedCaseStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
