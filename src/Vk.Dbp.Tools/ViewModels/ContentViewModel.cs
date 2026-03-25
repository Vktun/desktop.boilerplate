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
        private string _DenText;
        public string DenText
        {
            get { return _DenText; }
            set { SetProperty(ref _DenText, value); }
        }

        private string _EncText;
        public string EncText
        {
            get { return _EncText; }
            set { SetProperty(ref _EncText, value); }
        }

        private DelegateCommand<string> _sm4Command;
        public DelegateCommand<string> Sm4Command => _sm4Command ?? (_sm4Command = new DelegateCommand<string>(ExecuteSm4Command));
        public ContentViewModel()
        {
            
        }
      

        private void ExecuteSm4Command(string parameter)
        {
            if (parameter == "1")
            {
                EncText = SM4.Encrypt(DenText);
            }
            else if (parameter == "2")
            {
                DenText = SM4.Decrypt(EncText);
            }

        }
    }
}
