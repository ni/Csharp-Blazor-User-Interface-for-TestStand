// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Runs only in Blazor Server (server-side). BlockingCollection and threading primitives are supported on the server and this type is not executed in the browser.", Scope = "type", Target = "~T:NationalInstruments.TestStand.WebOI.UI.Services.SequencingObserver")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "For better understanding and cleaner code", Scope = "namespaceanddescendants", Target = "~N:NationalInstruments.TestStand.WebOI.UI.Components")]
[assembly: SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "For better understanding and cleaner code", Scope = "member", Target = "~M:NationalInstruments.TestStand.WebOI.UI.Components.StepsPane.OnActionMenuBeforeToggle(NimbleBlazor.TableActionMenuToggleEventArgs)")]
[assembly: SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "For better understanding and cleaner code", Scope = "member", Target = "~M:NationalInstruments.TestStand.WebOI.UI.Services.SequenceExtensions.GetAllSteps(NationalInstruments.TestStand.WebOI.SharedDomain.Models.Sequence)~System.Collections.Generic.IEnumerable{NationalInstruments.Sequencing.V2.Step}")]
[assembly: SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "For better understanding and cleaner code", Scope = "member", Target = "~M:NationalInstruments.TestStand.WebOI.UI.Components.ExecutionStepsPane.OnActionMenuBeforeToggle(NimbleBlazor.TableActionMenuToggleEventArgs)")]
[assembly: SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "For better understanding and cleaner code", Scope = "member", Target = "~M:NationalInstruments.TestStand.WebOI.UI.Components.ExecutionStepsPane.UpdateTableBackgroundCssProperty")]
