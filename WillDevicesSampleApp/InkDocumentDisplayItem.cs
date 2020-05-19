using System.Threading;
using Wacom.Ink.Model;

namespace WillDevicesSampleApp
{
	class InkDocumentDisplayItem
	{
		public InkDocumentDisplayItem(string errorMessage)
		{
			Id = Interlocked.Increment(ref _documentCounter);
			Document = null;
			ErrorMessage = errorMessage;
		}

		public InkDocumentDisplayItem(InkDocument document)
		{
			Id = Interlocked.Increment(ref _documentCounter);
			Document = document;
			ErrorMessage = string.Empty;
		}

		public override string ToString()
		{
			if (Document != null)
			{
				int strokesCount = Document.GetStrokesCount();

				return $"Document {Id} ({strokesCount} strokes)";
			}
			else
			{
				return $"Document {Id} ({ErrorMessage})";
			}
		}

		public InkDocument Document { get; private set; }
		public int Id { get; private set; }
		public string ErrorMessage { get; private set; }

		private static int _documentCounter = 0;
	}
}