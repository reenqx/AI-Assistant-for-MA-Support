using AIforMAsupport.Api.Models;

namespace AIforMAsupport.Api.Services.Conversation;

public sealed record ConversationTurn(string Question, IReadOnlyList<KbCase> Context, string Answer);
