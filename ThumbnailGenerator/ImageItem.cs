using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ThumbnailGenerator
{
    public class ImageItem : INotifyPropertyChanged
    {
        private FileInfo file;
        private State state;

        public ImageItem(FileInfo file, State state)
        {
            this.file = file;
            this.state = state;
        }

        public FileInfo File { get => file; set => file = value; }
        public State State
        {
            get => state;
            set
            {
                state = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("State"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public enum State
    {
        Pending, Processing, Solved
    }
}
