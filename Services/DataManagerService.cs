#nullable disable
using egibi_api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EgibiQuestDB;
using EgibiCoreLibrary.Models;
using egibi_api.Data.Entities;

namespace egibi_api.Services
{
    public class DataManagerService
    {
        private readonly EgibiDbContext _db;
        private readonly ConfigOptions _configOptions;

        public DataManagerService(EgibiDbContext db, IOptions<ConfigOptions> configOptions)
        {
            _db = db;
            _configOptions = configOptions.Value;
        }

        public async Task<RequestResponse> GetDataProviders()
        {
            try
            {
                List<DataProvider> dataProviders = await _db.DataProviders
                    .Include("DataProviderType")
                    .ToListAsync();

                return new RequestResponse(dataProviders, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> GetDataProvider(int id)
        {
            try
            {
                var dataProvider = await _db.DataProviders
                    .Include("DataProviderType")
                    .FirstOrDefaultAsync(x => x.Id == id);
                return new RequestResponse(dataProvider, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteDataProvider(int id)
        {
            try
            {
                _db.Remove(_db.DataProviders
                    .Where(w => w.Id == id)
                    .FirstOrDefault());
                await _db.SaveChangesAsync();

                return new RequestResponse(id, 200, "Deleted");
            }
            catch (Exception ex)
            {
                return new RequestResponse(id, 500, "Problem Deleting", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> DeleteDataProviders(List<int> ids)
        {
            try
            {
                _db.RemoveRange(_db.DataProviders
                    .Where(w => ids.Contains(w.Id)));
                await _db.SaveChangesAsync();

                return new RequestResponse(ids, 200, "Deleted");
            }
            catch (Exception ex)
            {
                return new RequestResponse(ids, 500, "Problem Deleting", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveDataProvider(DataProvider dataProvider)
        {
            if (dataProvider.Id == 0)
                return await CreateNewDataProvider(dataProvider);
            else
                return await UpdateExistingDataProvider(dataProvider);
        }

        public async Task<RequestResponse> GetDataProviderTypes()
        {
            try
            {
                List<DataProviderType> dataProviderTypes = await _db.DataProviderTypes
                    .Where(w => w.IsActive)
                    .ToListAsync();

                return new RequestResponse(dataProviderTypes, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
        public async Task<RequestResponse> GetDataFrequencyTypes()
        {
            try
            {
                List<DataFrequencyType> dataFrequencyTypes = await _db.DataFrequencyTypes
                    .Where(w => w.IsActive)
                    .ToListAsync();

                return new RequestResponse(dataFrequencyTypes, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }
        public async Task<RequestResponse> GetDataFormatTypes()
        {
            try
            {
                List<DataFormatType> dataFormatTypes = await _db.DataFormatTypes
                    .Where(w => w.IsActive)
                    .ToListAsync();

                return new RequestResponse(dataFormatTypes, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        public async Task<RequestResponse> SaveFile(IFormFile file)
        {
            var ingester = new Ingester("connectionString");
            ingester.LoadCsv(file);

            return new RequestResponse(null, null, "OK");
        }


        private async Task<RequestResponse> CreateNewDataProvider(DataProvider dataProvider)
        {
            DataProvider newDataProvider = new DataProvider
            {
                Name = dataProvider.Name,
                Description = dataProvider.Description,
                Notes = dataProvider.Notes,
                DataProviderTypeId = dataProvider.DataProviderTypeId,
                DataFormatTypeId = dataProvider.DataFormatTypeId,
                DataFrequencyTypeId = dataProvider.DataFrequencyTypeId,
                IsActive = true,
                Start = dataProvider.Start,
                End = dataProvider.End,
                CreatedAt = DateTime.Now.ToUniversalTime(),
                LastModifiedAt = null
            };

            try
            {
                await _db.AddAsync(newDataProvider);
                await _db.SaveChangesAsync();

                return new RequestResponse(dataProvider, 200, "OK");
            }
            catch (Exception ex)
            {
                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }
        }

        private async Task<RequestResponse> UpdateExistingDataProvider(DataProvider dataProvider)
        {
            DataProvider existingDataProvider = await _db.DataProviders
                .Where(w => w.Id == dataProvider.Id)
                .FirstOrDefaultAsync();

            existingDataProvider.Name = dataProvider.Name;
            existingDataProvider.Description = dataProvider.Description;
            existingDataProvider.Notes = dataProvider.Notes;
            existingDataProvider.DataProviderTypeId = dataProvider.DataProviderTypeId;
            existingDataProvider.DataFormatTypeId = dataProvider.DataFormatTypeId;
            existingDataProvider.DataFrequencyTypeId = dataProvider.DataFrequencyTypeId;
            existingDataProvider.IsActive = dataProvider.IsActive;
            existingDataProvider.Start = dataProvider.Start;
            existingDataProvider.End = dataProvider.End;
            existingDataProvider.LastModifiedAt = DateTime.Now.ToUniversalTime();

            try
            {
                _db.Update(existingDataProvider);
                await _db.SaveChangesAsync();

                return new RequestResponse(dataProvider, 200, "OK");
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException?.Message;

                return new RequestResponse(null, 500, "There was an error", new ResponseError(ex));
            }

        }
    }
}