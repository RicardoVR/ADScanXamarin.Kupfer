using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ADScan.Client.Models;
using SQLite;
using System.Linq;

namespace ADScan.Client.Data
{
    public class ADScanDatabase
    {
        static SQLiteAsyncConnection Database;

        public static readonly AsyncLazy<ADScanDatabase> Instance = new AsyncLazy<ADScanDatabase>(async () =>
        {
            var instance = new ADScanDatabase();
            CreateTableResult result = await Database.CreateTableAsync<MassiveDevice>();
            await Database.CreateTableAsync<Models.DeviceConfiguration>();
            await Database.CreateTableAsync<Models.Filter>();
            await Database.CreateTableAsync<Models.FilterDevice>();
            await Database.CreateTableAsync<Models.DeviceMessage>();

            return instance;
        });

        public ADScanDatabase()
        {
            Database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        }

        public async Task<bool> Delete<T>(T item) 
        {
            try
            {

                var result = await Database.DeleteAsync(item);
                return result == 1;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<MassiveDevice> GetDevice(string address)
        {
            try
            {
                var data = await Database.Table<MassiveDevice>().ToListAsync();

                return data.FirstOrDefault(c => c.Address == address);
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        public async Task<Models.DeviceConfiguration> GetConfiguration(string index)
        {
            try
            {
                var data = await Database.Table<Models.DeviceConfiguration>().ToListAsync();

                return data.FirstOrDefault(c => c.Index == index);
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        public async Task<List<T>> GetAll<T>() where T : new()
        {
            try
            {
                return await Database.Table<T>().ToListAsync();
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }

        public async Task<bool> Persist<T>(T item) 
        {
            try
            {
                await Database.InsertAsync(item);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> PersistAll<T>(IEnumerable<T> items) 
        {
            try
            {

                int cnt = 0;
                foreach (var i in items)
                {
                    await Database.InsertAsync(i);
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> Update<T>(T item)
        {
            try
            {
                await Database.UpdateAsync(item);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteFilterByName(string item)
        {
            try
            {
                var data = await Database.Table<Filter>().ToListAsync();
                var filter = data.FirstOrDefault(c => c.Device == item);

                if (filter != null)
                {
                    var result = await Database.DeleteAsync(filter);
                    return result == 1;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> DeleteDeviceFilterByMac(string mac)
        {
            try
            {
                var data = await Database.Table<FilterDevice>().ToListAsync();
                var filter = data.FirstOrDefault(c => c.Mac == mac);

                if (filter != null)
                {
                    var result = await Database.DeleteAsync(filter);
                    return result == 1;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> ClearMessages()
        {
            try
            {
                await Database.DeleteAllAsync<DeviceMessage>();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> IsMessageValid(string macAddress, string raw)
        {
            try
            {
                var message = await Database.Table<DeviceMessage>().Where(c => c.MacAddress == macAddress).OrderByDescending(c => c.CreatedOn).FirstOrDefaultAsync();

                if (message != null)
                {
                    if (message.Raw != raw)
                        return true;
                }
                else {
                    return true;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }
    }
}

