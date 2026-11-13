using Paowa.Core.Foundation.Services;
using Paowa.Localization;

namespace Game.Foundation.Localization
{
    /// <summary>
    /// Localized text by key through Core's <see cref="ILocalizationService"/> (tables in
    /// <c>Resources/Paowa/Localization</c>). A key missing from the table shows as <c>#key#</c>. Without the localization
    /// module the key itself is returned; while the module is still loading its first language the text is empty (views
    /// write it again on <see cref="LanguageChangedEvent"/>, which Core publishes when that language is ready), so no
    /// <c>#key#</c> flashes with an asynchronous backend. An empty key gives an empty string.
    /// </summary>
    public static class Loc
    {
        /// <summary>
        /// False while the localization module exists but has not loaded a language yet; true when it is ready or absent.
        /// </summary>
        public static bool IsReady => !CoreServiceLocator.TryGet(out ILocalizationService localization) || localization.IsReady;

        /// <summary>Text of <paramref name="key"/> in the active language.</summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (!CoreServiceLocator.TryGet(out ILocalizationService localization))
            {
                return key;
            }

            return localization.IsReady ? localization.Get(key) : string.Empty;
        }

        /// <summary>Text of <paramref name="key"/> with <c>{0}</c>, <c>{1}</c>... replaced by <paramref name="arguments"/>.</summary>
        public static string Format(string key, params object[] arguments)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (!CoreServiceLocator.TryGet(out ILocalizationService localization))
            {
                return key;
            }

            return localization.IsReady ? localization.Format(key, arguments) : string.Empty;
        }
    }
}
