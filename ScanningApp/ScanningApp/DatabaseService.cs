using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace ScanningApp
{
    public static class DatabaseService
    {
        static SQLiteAsyncConnection db;

        // Initialize the DB only once
        public static async Task Init()
        {
            if (db != null) return;

            var path = Path.Combine(FileSystem.AppDataDirectory, "items.db");
            db = new SQLiteAsyncConnection(path);

            await db.CreateTableAsync<ScannedItem>();
        }

        // Add item if it doesn't already exist
        public static async Task AddItem(string code)
        {
            await Init();

            var existing = await db.Table<ScannedItem>()
                                   .Where(x => x.Code == code)
                                   .FirstOrDefaultAsync();

            if (existing == null)
            {
                await db.InsertAsync(new ScannedItem { Code = code });
            }
        }

        // Get all items
        public static async Task<List<ScannedItem>> GetItems()
        {
            await Init();
            return await db.Table<ScannedItem>().ToListAsync();
        }

        // Clear all items
        public static async Task ClearItems()
        {
            await Init();
            await db.DeleteAllAsync<ScannedItem>();
        }

        public static async Task DeleteItem(int id)
        {
            await Init();
            await db.DeleteAsync<ScannedItem>(id);
        }
    }
}
