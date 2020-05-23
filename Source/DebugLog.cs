namespace Reminders
{
	static class Log
	{
		[System.Diagnostics.Conditional("DEBUG")]
		public static void Debug(string msg)
		{
			Message(msg);
		}

		public static void Message(string msg )
		{
			Verse.Log.Message( $"Reminders :: {msg}");
		}
	}
}
