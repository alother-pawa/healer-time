using System;

namespace Game.Contracts.Presentation
{
    /// <summary>
    /// Texts of one confirmation dialog, as localization keys. Without a cancel key the dialog is a notice with a single
    /// button (closing it still answers false).
    /// </summary>
    public sealed class ConfirmRequest
    {
        /// <summary>Creates a request; title, message and confirm keys are required.</summary>
        /// <param name="messageArguments">Values for <c>{0}</c>, <c>{1}</c>... in the message; null for a plain message.</param>
        public ConfirmRequest(string titleKey, string messageKey, string confirmKey, string cancelKey,
            params object[] messageArguments)
        {
            if (string.IsNullOrEmpty(titleKey))
            {
                throw new ArgumentException("A confirmation needs a title key.", nameof(titleKey));
            }

            if (string.IsNullOrEmpty(messageKey))
            {
                throw new ArgumentException("A confirmation needs a message key.", nameof(messageKey));
            }

            if (string.IsNullOrEmpty(confirmKey))
            {
                throw new ArgumentException("A confirmation needs a confirm button key.", nameof(confirmKey));
            }

            TitleKey = titleKey;
            MessageKey = messageKey;
            ConfirmKey = confirmKey;
            CancelKey = cancelKey ?? string.Empty;
            MessageArguments = messageArguments != null && messageArguments.Length > 0 ? messageArguments : null;
        }

        /// <summary>Key of the title.</summary>
        public string TitleKey { get; }

        /// <summary>Key of the message (a format when <see cref="MessageArguments"/> is set).</summary>
        public string MessageKey { get; }

        /// <summary>Key of the button that answers true.</summary>
        public string ConfirmKey { get; }

        /// <summary>Key of the button that answers false; empty for a single-button notice.</summary>
        public string CancelKey { get; }

        /// <summary>Format arguments of the message; null for a plain message.</summary>
        public object[] MessageArguments { get; }

        /// <summary>True when the dialog shows a cancel button.</summary>
        public bool HasCancel => CancelKey.Length > 0;
    }
}
