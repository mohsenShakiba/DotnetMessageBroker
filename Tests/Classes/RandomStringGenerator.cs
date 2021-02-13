using System;
using System.Linq;

namespace Tests.Classes
{
    public static class RandomStringGenerator
    {
        public static string Generate(int length, Random random = null)
        {
            random ??= new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}