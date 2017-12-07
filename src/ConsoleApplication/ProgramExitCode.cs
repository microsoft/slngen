namespace SlnGen
{
    public enum ProgramExitCode
    {
        Success = 0,
        NoProjectsFoundError = -1,
        TooManyProjectsFoundError = -2,
        BadOrMissingArgumentError = -3,
        BadProjectNameError = -4,
        ValidationError = -6,
        BadProjectGuidsError = -7
    }
}