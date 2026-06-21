using Dabp.Utils.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dabp.Tools.ViewModels
{
    public class ContentViewModel : BindableBase
    {
        private string _denText = string.Empty;
        public string DenText
        {
            get => _denText;
            set
            {
                if (_denText == value)
                {
                    return;
                }

                _denText = value;
                RaisePropertyChanged();
            }
        }

        private string _encText = string.Empty;
        public string EncText
        {
            get => _encText;
            set
            {
                if (_encText == value)
                {
                    return;
                }

                _encText = value;
                RaisePropertyChanged();
            }
        }

        private string _sm4Key = string.Empty;
        public string Sm4Key
        {
            get => _sm4Key;
            set
            {
                if (_sm4Key == value)
                {
                    return;
                }

                _sm4Key = value;
                RaisePropertyChanged();
            }
        }

        private DelegateCommand<string>? _sm4Command;
        public DelegateCommand<string> Sm4Command => _sm4Command ??= new DelegateCommand<string>(ExecuteSm4Command);

        public ContentViewModel()
        {
        }

        private void ExecuteSm4Command(string parameter)
        {
            if (string.IsNullOrWhiteSpace(Sm4Key))
                return;

            if (parameter == "1")
            {
                EncText = SM4.Encrypt(DenText, Sm4Key);
            }
            else if (parameter == "2")
            {
                DenText = SM4.Decrypt(EncText, Sm4Key);
            }
        }
    }
}
