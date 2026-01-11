using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace colors.Config;

internal class FirebaseConfig
{
    // Replace these with YOUR Firebase configuration values
    public static string DatabaseUrl => 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FIREBASE_DATABASE_URL")) 
            ? Environment.GetEnvironmentVariable("FIREBASE_DATABASE_URL")! 
            : Secrets.DatabaseUrl ?? string.Empty;

    public static string StorageBucket => 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FIREBASE_STORAGE_BUCKET")) 
            ? Environment.GetEnvironmentVariable("FIREBASE_STORAGE_BUCKET")! 
            : Secrets.StorageBucket ?? string.Empty;

    public static string ApiKey => 
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FIREBASE_API_KEY")) 
            ? Environment.GetEnvironmentVariable("FIREBASE_API_KEY")! 
            : Secrets.ApiKey ?? string.Empty;

    // For now, we'll use a simple user ID
    // Later we can implement proper authentication
    public const string UserId = "user_001"; // Temporary for testing
}
