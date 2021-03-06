using Verse;

namespace Reminders
{
    public static class I18n
    {
        public static string Translate( string key, params NamedArgument[] args )
        {
            return Key( key ).Translate( args ).Resolve();
        }

        private static string Key( string key )
        {
            return $"Reminders.{key}";
        }
    }
}