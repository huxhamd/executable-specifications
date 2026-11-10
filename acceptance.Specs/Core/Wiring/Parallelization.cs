// xUnit's default runs each class's specs one at a time, which would hide a
// spec that depends on another spec. How many run at once is set per channel
// in scripts/run-acceptance-tests.ps1.
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.All)]
