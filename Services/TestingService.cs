#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using EgibiCoreLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Collections.Specialized;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Reflection;
using EgibiCoreLibrary;


namespace egibi_api.Services
{
    public class TestingService
    {
        public async Task<RequestResponse> RunGeoDateTimeDataTest()
        {
            var result = GeoDateTimeDataHandler.GetGeoDateTimeData();

            return null;
        }
    }
}
