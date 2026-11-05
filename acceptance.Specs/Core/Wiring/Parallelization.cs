// Runs every spec in parallel, including specs in the same class. xUnit's
// default runs each class's specs one at a time, which would hide a spec
// that depends on another spec, or on the state of the whole system.
// How many specs run at once is set per channel, in
// scripts/run-acceptance-tests.ps1. This is harness wiring, owned by the
// harness agent like every project's Core/Wiring: specs don't need it.
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.All)]
