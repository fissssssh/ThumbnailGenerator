using System.ComponentModel;

namespace ThumbnailGenerator
{
    public class ThreadCounter : INotifyPropertyChanged
    {
        private readonly object _obj = new object();

        private ThreadCounter()
        {
        }

        private int count = 0;

        public int Count
        {
            get => count;
            private set
            {
                count = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Count"));
            }
        }

        private static ThreadCounter _instanse;

        public event PropertyChangedEventHandler PropertyChanged;

        public static ThreadCounter Instance
        {
            get
            {
                if (_instanse == null)
                {
                    _instanse = new ThreadCounter();
                }
                return _instanse;
            }
        }

        public void Increase(int num)
        {
            lock (_obj)
            {
                Count += num;
            }
        }

        public void Reduce(int num)
        {
            lock (_obj)
            {
                Count -= num;
            }
        }
        public void Reset()
        {
            lock (_obj)
            {
                Count = 0;
            }
        }
    }
}