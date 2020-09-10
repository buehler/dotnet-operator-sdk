namespace KubeOps.Operator.Leadership
{
    public enum LeaderState
    {
        /// <summary>
        /// The state of the leadership has yet to be determined.
        /// </summary>
        None,

        /// <summary>
        /// The instance is a candidate and another instance is actually leading.
        /// </summary>
        Candidate,

        /// <summary>
        /// The instance is the actual leader.
        /// </summary>
        Leader,
    }
}
