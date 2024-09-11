namespace RedUtils
{
	/// <summary>An action to be performed by our bot</summary>
	public interface IAction
	{
		/// <summary>Whether the action has finished</summary>
		public bool Finished { get; }
		/// <summary>Whether the action can be interrupted</summary>
		public bool Interruptible { get; }
		/// <summary>Whether the action is primarily used to navigate the field</summary>
		public bool Navigational { get; }

		/// <summary>Performs this action</summary>
		public void Run(RUBot bot);
	}
}
