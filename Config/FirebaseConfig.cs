using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace colors.Config
{
    internal class FirebaseConfig
    {
        // Replace these with YOUR Firebase configuration values
        public const string DatabaseUrl = "https://colors-001-default-rtdb.europe-west1.firebasedatabase.app";
        public const string StorageBucket = "colors-001.firebasestorage.app";
        public const string ApiKey = "AIzaSyDofhypXXwZLrCWKeu7GT43E54qpl1R_Ac"; // Your API key

        // For now, we'll use a simple user ID
        // Later we can implement proper authentication
        public const string UserId = "user_001"; // Temporary for testing

    }
}
