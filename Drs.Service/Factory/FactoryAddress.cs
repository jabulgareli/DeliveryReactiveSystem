﻿using System;
using System.Collections.Generic;
using System.Linq;
using Drs.Model.Address;
using Drs.Model.Constants;
using Drs.Model.Settings;
using Drs.Model.Shared;
using Drs.Model.Store;
using Drs.Repository.Entities;
using Drs.Service.Configuration;

namespace Drs.Service.Factory
{
    public static class FactoryAddress
    {
        public static IQueryable<AddressResponseSearch> GetQueryToExecByZipCode(CallCenterEntities dbEntities, String zipCode, bool startWith = false)
        {

            string regionChild = GetRegionChildByZipCode();


            switch (regionChild)
            {
                case AddressConstants.REGION_A:
                {
                    System.Linq.Expressions.Expression<Func<RegionA, bool>> linqExp;
                    if (startWith) linqExp = e => e.ZipCode.Code.StartsWith(zipCode); else linqExp = e => e.ZipCode.Code == zipCode;

                    return dbEntities.RegionA.Where(linqExp).Select(e => new AddressResponseSearch
                    {
                        Country = new ListItemModel { IdKey = e.CountryId, Value = e.Country.Name },
                        RegionA = new ListItemModel { IdKey = e.RegionId, Value = e.Name },
                        ZipCode = new ListItemModel { IdKey = e.ZipCodeId, Value = e.ZipCode.Code}
                    }).Take(50);                    
                }
                case AddressConstants.REGION_B:
                {
                    System.Linq.Expressions.Expression<Func<RegionB, bool>> linqExp;
                    if (startWith) linqExp = e => e.ZipCode.Code.StartsWith(zipCode); else linqExp = e => e.ZipCode.Code == zipCode;

                    return dbEntities.RegionB.Where(linqExp).Select(e => new AddressResponseSearch
                        {
                            Country = new ListItemModel { IdKey = e.CountryId, Value = e.Country.Name },
                            RegionA = new ListItemModel { IdKey = e.RegionArId, Value = e.RegionA.Name },
                            RegionB = new ListItemModel { IdKey = e.RegionId, Value = e.Name },
                            ZipCode = new ListItemModel { IdKey = e.ZipCodeId, Value = e.ZipCode.Code }
                        }).Take(50);
                }
                case AddressConstants.REGION_C:
                {
                    System.Linq.Expressions.Expression<Func<RegionC, bool>> linqExp;
                    if (startWith) linqExp = e => e.ZipCode.Code.StartsWith(zipCode); else linqExp = e => e.ZipCode.Code == zipCode;

                    return dbEntities.RegionC.Where(linqExp).Select(e => new AddressResponseSearch
                    {
                        Country = new ListItemModel { IdKey = e.CountryId, Value = e.Country.Name },
                        RegionA = new ListItemModel { IdKey = e.RegionArId, Value = e.RegionA.Name },
                        RegionB = new ListItemModel { IdKey = e.RegionBrId, Value = e.RegionB.Name },
                        RegionC = new ListItemModel { IdKey = e.RegionId, Value = e.Name },
                        ZipCode = new ListItemModel { IdKey = e.ZipCodeId, Value = e.ZipCode.Code }
                    }).Take(50);
                }
                case AddressConstants.REGION_D:
                {
                    System.Linq.Expressions.Expression<Func<RegionD, bool>> linqExp;
                    if (startWith) linqExp = e => e.ZipCode.Code.StartsWith(zipCode); else linqExp = e => e.ZipCode.Code == zipCode;

                    return dbEntities.RegionD.Where(linqExp).Select(e => new AddressResponseSearch
                    {
                        Country = new ListItemModel { IdKey = e.CountryId, Value = e.Country.Name },
                        RegionA = new ListItemModel { IdKey = e.RegionArId, Value = e.RegionA.Name },
                        RegionB = new ListItemModel { IdKey = e.RegionBrId, Value = e.RegionB.Name },
                        RegionC = new ListItemModel { IdKey = e.RegionCrId, Value = e.RegionC.Name },
                        RegionD = new ListItemModel { IdKey = e.RegionId, Value = e.Name },
                        ZipCode = new ListItemModel { IdKey = e.ZipCodeId, Value = e.ZipCode.Code }
                    }).Take(50);
                }
            }

            throw new ArgumentNullException(String.Empty + "No está correctamente configurada la jerarquía de direcciones");
        }

        public static String GetRegionChildByZipCode()
        {
            AddressHierarchyInfo addressHierarchy;
            if (SettingAddress.DicAddressHierarchy.TryGetValue(AddressConstants.ZIP_CODE, out addressHierarchy) == false)
            {
                throw new ArgumentNullException(String.Empty + "No se ha configurado la jerarquía de direcciones");
            }
            return addressHierarchy.RegionChild;
        }


        public static List<String> GetAddressHierarchyOrderById()
        {
            return SettingAddress.DicAddressHierarchy.Select(e => e.Key).ToList();

        }

        public static IQueryable<ListItemModel> GetQueryToFillNextListByName(CallCenterEntities dbEntities, string sNextList, int iIdSelected)
        {
            sNextList = sNextList.ToUpper();

            switch (sNextList)
            {
                case SettingsData.Constants.Control.CONTROL_COUNTRY:
                    return dbEntities.Country.OrderBy(e => e.CountryId).Select(e => new ListItemModel { IdKey = e.CountryId, Value = e.Name });
                case SettingsData.Constants.Control.CONTROL_REGION_A:
                    return dbEntities.RegionA.Where(e => e.CountryId == iIdSelected).OrderBy(e => e.Name).Select(e => new ListItemModel { IdKey = e.RegionId, Value = e.Name });
                case SettingsData.Constants.Control.CONTROL_REGION_B:
                    return dbEntities.RegionB.Where(e => e.RegionArId == iIdSelected).OrderBy(e => e.Name).Select(e => new ListItemModel { IdKey = e.RegionId, Value = e.Name });
                case SettingsData.Constants.Control.CONTROL_REGION_C:
                    return dbEntities.RegionC.Where(e => e.RegionBrId == iIdSelected).OrderBy(e => e.Name).Select(e => new ListItemModel { IdKey = e.RegionId, Value = e.Name });
                case SettingsData.Constants.Control.CONTROL_REGION_D:
                    return dbEntities.RegionD.Where(e => e.RegionCrId == iIdSelected).OrderBy(e => e.Name).Select(e => new ListItemModel { IdKey = e.RegionId, Value = e.Name });
            }

            throw new ArgumentNullException(String.Empty + "No está configurada la información de la jerarquía de direcciones");
        }

        public static ListItemModel GetQueryToGetByZipCodeId(CallCenterEntities dbEntities, string lastRegion, AddressInfoModel model)
        {
            lastRegion = lastRegion.ToUpper();

            switch (lastRegion)
            {
                case SettingsData.Constants.Control.CONTROL_REGION_A:
                    return dbEntities.RegionA.Where(e => e.RegionId == model.RegionA.IdKey).Select(e => new ListItemModel { IdKey = e.ZipCodeId, Value = e.ZipCode.Code }).Single();
                case SettingsData.Constants.Control.CONTROL_REGION_B:
                    return dbEntities.RegionB.Where(e => e.RegionId == model.RegionB.IdKey).Select(e => new ListItemModel { IdKey = e.ZipCodeId, Value = e.ZipCode.Code }).Single();
                case SettingsData.Constants.Control.CONTROL_REGION_C:
                    return dbEntities.RegionC.Where(e => e.RegionId == model.RegionC.IdKey).Select(e => new ListItemModel { IdKey = e.ZipCodeId, Value = e.ZipCode.Code }).Single();
                case SettingsData.Constants.Control.CONTROL_REGION_D:
                    return dbEntities.RegionD.Where(e => e.RegionId == model.RegionD.IdKey).Select(e => new ListItemModel { IdKey = e.ZipCodeId, Value = e.ZipCode.Code }).Single();
                
            }

            throw new ArgumentNullException(String.Empty + "No está configurada la información de la jerarquía de direcciones para los códigos");
        }

        public static List<StoreModel> GetQueryToSearchStore(CallCenterEntities dbEntities, string franchiseCode, AddressInfoModel model, out int franchiseId)
        {
            franchiseId = dbEntities.Franchise.Where(e => e.IsObsolete == false && e.Code == franchiseCode).Select(e => e.FranchiseId).Single();

            var id = franchiseId;
            var query = dbEntities.StoreAddressDistribution.Where(e => e.FranchiseStore.FranchiseId == id);

            if (SettingsData.Store.ByCountry)
            {
                query = query.Where(e => e.CountryId == model.Country.IdKey);
            }
            if (SettingsData.Store.ByRegionA)
            {
                query = query.Where(e => e.RegionArId == model.RegionA.IdKey);
            }
            if (SettingsData.Store.ByRegionB)
            {
                query = query.Where(e => e.RegionBrId == model.RegionB.IdKey);
            }
            if (SettingsData.Store.ByRegionC)
            {
                query = query.Where(e => e.RegionCrId == model.RegionC.IdKey);
            }
            if (SettingsData.Store.ByRegionD)
            {
                query = query.Where(e => e.RegionDrId == model.RegionD.IdKey);
            }
            if (SettingsData.Store.ByZipCode)
            {
                query = query.Where(e => e.ZipCodeId == model.ZipCode.IdKey);
            }

            return query.OrderByDescending(e => e.Importance).Select(e => 
                new StoreModel
                {
                    Key = e.FranchiseStoreId.ToString(),
                    IdKey = e.FranchiseStoreId, 
                    Value = e.FranchiseStore.Name, 
                    MainAddress = e.FranchiseStore.Address.MainAddress,
                    LstPhones = e.FranchiseStore.FranchisePhone.Select(i => i.Phone).ToList(),
                    WsAddress = e.FranchiseStore.WsAddress
                }).ToList();
        }
    }
}
