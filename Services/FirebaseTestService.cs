using System;
using System.Threading.Tasks;
using colors.Config;
using Firebase.Database;
using Firebase.Database.Query;

namespace colors.Services;

public class FirebaseTestService
{
    private readonly FirebaseClient _firebaseClient;

    public FirebaseTestService(FirebaseClient firebaseClient)
    {
        _firebaseClient = firebaseClient ?? throw new ArgumentNullException(nameof(firebaseClient));
    }
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var testRef = _firebaseClient.Child("connection");
            // Attempt to read from the database to test connection
            await _firebaseClient
                .Child(".info/connected")
                .OnceSingleAsync<bool>();            
            // Cleanup test data
            // Cleanup test data


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