namespace AIforMAsupport.Api.Services.KnownScripts;

// A real MA maintenance script (see /KnownScripts/*.sql), as opposed to the KB cases, which
// mostly only ever record a script's *name* - not its SQL - since MA never pasted the full
// command into Mantis. This is the actual answer to the "KB ไม่มีเนื้อหา SQL จริง" gap that kept
// coming up in testing.
public sealed record KnownScript(string Name, string Description, string SqlContent);
