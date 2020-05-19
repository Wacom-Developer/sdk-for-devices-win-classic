using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace WillDevicesSampleApp
{
    class DevicePropertyValuePair : INotifyPropertyChanged
    {
		private string _propertyName;
		private string _propertyValue;

		public string PropertyName
		{
			get
			{
				return _propertyName;
			}
			set
			{
				_propertyName = value;
				NotifyPropertyChanged("PropertyName");
			}
		}

		public string PropertyValue
		{
			get
			{
				return (_propertyValue == null) ? "N/A" : _propertyValue;
			}
			set
			{
				_propertyValue = value;
				NotifyPropertyChanged("PropertyValue");
			}
		}
		
		public DevicePropertyValuePair(string name)
		{
			_propertyName = name;
			_propertyValue = null;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
