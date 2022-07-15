using System.Runtime.InteropServices;


namespace MTConnectSharp
{
	[ComVisible(true)]
	[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
	public interface IClientEvents
	{
		void DataItemChanged(object sender, DataItemChangedEventArgs e);

	}
}
