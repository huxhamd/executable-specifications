$changes = git status --porcelain -- acceptance/
if ($changes) {
    [Console]::Error.WriteLine(
        "Files under acceptance/ changed. They are owned by the human. " +
        "Undo every change (git restore acceptance/, and delete any new files), " +
        "then explain why you changed them.`n$changes")
    exit 2
}
exit 0