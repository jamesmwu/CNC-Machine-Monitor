using System;
using System.Runtime.InteropServices;


namespace MTConnectSharp
{
	/// <summary>
	/// Data for the DataItemChanged Event
	/// </summary>
	[ComVisibleAttribute(true)]

	public class DataItemChangedEventArgs : EventArgs, IDataItemChangedEventArgs
	{
		/// <summary>
		/// DataItem that was changed
		/// </summary>
		public DataItem DataItem { get; set; }

		/// <summary>
		/// Class Constructor
		/// </summary>
		/// <param name="dataItem">The DataItem that was changed</param>
		internal DataItemChangedEventArgs(DataItem dataItem)
		{
			DataItem = dataItem;
		}
	}
}
