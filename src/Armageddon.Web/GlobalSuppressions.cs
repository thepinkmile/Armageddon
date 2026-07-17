// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "ASP0028:Consider using ListenAnyIP() instead of Listen(IPAddress.Any)", Justification = "Not using IPv6 as on Raspberry Pi")]
[assembly: SuppressMessage("Spellchecker", "CRRSP08:A misspelled word has been found", Justification = "UK English")]
