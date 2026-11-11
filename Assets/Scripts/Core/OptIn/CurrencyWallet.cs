using System;
using System.Collections.Generic;

namespace BlindDelivery.Core.OptIn
{
    /// <summary>What one grant or spend did to one currency. Past-tense fact shape.</summary>
    public struct CurrencyChange
    {
        public string currency;

        public int amount;

        public int balanceAfter;

        /// <summary>True when the change was a spend, false when it was a grant.</summary>
        public bool spent;
    }

    /// <summary>
    /// Bonus system 1 (opt-in file, not base wiring): soft/hard currency wallet. Currencies are
    /// plain string ids ("coins", "gems") â€” the wallet does not know which is which, so the same
    /// code backs any economy. Every mutation goes through <see cref="Grant"/> or
    /// <see cref="TrySpend"/> (one way to change state; never negative, insufficient funds
    /// refused); listeners hear the <see cref="Changed"/> fact, not commands.
    ///</summary>
    public sealed class CurrencyWallet
    {
        readonly Dictionary<string, int> balances = new Dictionary<string, int>();

        /// <summary>Raised after a successful grant or spend. Fires zero times for refusals.</summary>
        public event Action<CurrencyChange> Changed;

        public IReadOnlyDictionary<string, int> Balances => balances;

        /// <summary>Current balance of a currency; 0 for a currency nothing was ever granted to.</summary>
        public int GetBalance(string currency)
        {
            return balances.TryGetValue(Normalize(currency), out int balance) ? balance : 0;
        }

        /// <summary>Adds currency. Rejects non-positive amounts â€” a grant of zero is a bug upstream.</summary>
        public void Grant(string currency, int amount)
        {
            RequireCurrency(currency);
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "A grant must be positive.");
            }

            string id = Normalize(currency);
            balances[id] = GetBalance(id) + amount;
            Changed?.Invoke(new CurrencyChange
            {
                currency = id,
                amount = amount,
                balanceAfter = balances[id],
                spent = false,
            });
        }

        /// <summary>
        /// Spends currency: true and fired when affordable, false and silent when not. The balance
        /// is never allowed to go negative, so an overdraft is impossible by construction.
        /// </summary>
        public bool TrySpend(string currency, int amount)
        {
            RequireCurrency(currency);
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "A spend must be positive.");
            }

            string id = Normalize(currency);
            int balance = GetBalance(id);
            if (balance < amount)
            {
                return false;
            }

            balances[id] = balance - amount;
            Changed?.Invoke(new CurrencyChange
            {
                currency = id,
                amount = amount,
                balanceAfter = balances[id],
                spent = true,
            });
            return true;
        }

        static string Normalize(string currency)
        {
            RequireCurrency(currency);
            return currency.Trim().ToLowerInvariant();
        }

        static void RequireCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new ArgumentException("A currency id is required.", nameof(currency));
            }
        }
    }
}

