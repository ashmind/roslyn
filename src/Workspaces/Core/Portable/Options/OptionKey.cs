﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    public struct OptionKey : IEquatable<OptionKey>
    {
        public IOption Option { get; }
        public string Language { get; }

        internal object DefaultValue => ((IOption2)Option).GetDefaultValue(Language);

        public OptionKey(IOption option, string language = null)
        {
            if (option == null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            if (language != null && !option.IsPerLanguage)
            {
                throw new ArgumentException(WorkspacesResources.InvalidLanguageNameOption);
            }
            else if (language == null && option.IsPerLanguage)
            {
                throw new ArgumentNullException(WorkspacesResources.InvalidLanguageNameOption2);
            }

            this.Option = option;
            this.Language = language;
        }

        public override bool Equals(object obj)
        {
            if (obj is OptionKey)
            {
                return Equals((OptionKey)obj);
            }

            return false;
        }

        public bool Equals(OptionKey other)
        {
            return Option == other.Option && Language == other.Language;
        }

        public override int GetHashCode()
        {
            var hash = Option.GetHashCode();

            if (Language != null)
            {
                hash = Hash.Combine(Language.GetHashCode(), hash);
            }

            return hash;
        }

        public static bool operator ==(OptionKey left, OptionKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(OptionKey left, OptionKey right)
        {
            return !left.Equals(right);
        }
    }
}
