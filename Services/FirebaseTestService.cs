using System;
using System.Threading.Tasks;
using colors.Config;
using Firebase.Database;
using Firebase.Database.Query;

namespace colors.Services
{
    public class FirebaseTestService
    {
        private readonly FirebaseClient _firebaseClient;

        public FirebaseTestService()
        {
            _firebaseClient = new FirebaseClient(FirebaseConfig.DatabaseUrl);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Attempt to write a test entry to the database
                await _firebaseClient
                    .Child("connection")
                    .PostAsync(new { message = "Connection successful", timestamp = DateTime.UtcNow });

                System.Console.WriteLine("✅ Firebase connection successful! wrote test data.");
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Firebase connection failed: {ex.Message}");
                return false;
            }
        }
    }
}
