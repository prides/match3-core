namespace Match3Core
{
    public class Logger
    {
        public delegate void SimpleEventDelegate(string message);

        public event SimpleEventDelegate OnDebugMessage;
        public event SimpleEventDelegate OnWarningMessage;
        public event SimpleEventDelegate OnErrorMessage;

        private static Logger instance;
        public static Logger Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new Logger();
                }
                return instance;
            }
        }

        internal void Message(string message)
        {
            if (null != OnDebugMessage)
            {
                OnDebugMessage(message);
            }
        }

        internal void Warning(string message)
        {
            if (null != OnWarningMessage)
            {
                OnWarningMessage(message);
            }
        }

        internal void Error(string message)
        {
            if (null != OnErrorMessage)
            {
                OnErrorMessage(message);
            }
        }
    }
}