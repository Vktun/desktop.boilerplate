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
            set => SetProperty(ref _denText, value);
        }

        private string _encText = string.Empty;
        public string EncText
        {
            get => _encText;
            set => SetProperty(ref _encText, value);
        }

        private string _sm4Key = "DabpSm4DefaultKey";
        public string Sm4Key
        {
            get => _sm4Key;
            set => SetProperty(ref _sm4Key, value);
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
