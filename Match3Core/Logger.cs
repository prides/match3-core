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

        public void Message(string message)
        {
            if (null != OnDebugMessage)
            {
                OnDebugMessage(message);
            }
        }

        public void Warning(string message)
        {
            if (null != OnWarningMessage)
            {
                OnWarningMessage(message);
            }
        }

        public void Error(string message)
        {
            if (null != OnErrorMessage)
            {
                OnErrorMessage(message);
            }
        }
    }
}