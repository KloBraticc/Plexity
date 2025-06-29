﻿using Plexity.Enums;

namespace Plexity.Extensions
{
    static class CleanerOptionsEx
    {
        public static IReadOnlyCollection<CleanerOptions> Selections => new CleanerOptions[]
        {
            CleanerOptions.Never,
            CleanerOptions.OneDay,
            CleanerOptions.OneWeek,
            CleanerOptions.OneMonth,
            CleanerOptions.TwoMonths
        };
    }
}