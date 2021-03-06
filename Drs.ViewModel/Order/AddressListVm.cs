﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Drs.Infrastructure.Extensions.Enumerables;
using Drs.Infrastructure.Logging;
using Drs.Model.Address;
using Drs.Model.Constants;
using Drs.Model.Order;
using Drs.Model.Settings;
using Drs.Model.Shared;
using Drs.Service.ReactiveDelivery;
using Drs.Service.TransferDto;
using Drs.ViewModel.Main;
using Drs.ViewModel.Shared;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;

namespace Drs.ViewModel.Order
{
    public class AddressListVm : UcViewModelBase, IAddressListVm
    {
        private IUpsertAddressFoVm _upsertAddress;
        private readonly IReactiveDeliveryClient _client;
        private AddressInfoGrid _addressSelection;
        private Func<OrderModel> _orderModel;
        private Visibility _hasAdditionalLstStore;
        private ItemCatalog _pickUpStore;
        private bool _isGettingData;

        public AddressListVm(IUpsertAddressFoVm upsertAddressFo, IReactiveDeliveryClient client)
        {
            _upsertAddress = upsertAddressFo;
            _client = client;
            LstChildren.Add(_upsertAddress);
            Add = ReactiveCommand.CreateAsyncTask(Observable.Return(true), _ => OnAdd());
            Edit = ReactiveCommand.CreateAsyncTask(Observable.Return(true), OnEdit);
            Remove = ReactiveCommand.CreateAsyncTask(Observable.Return(true), OnRemove);
            RetrySaveItem = ReactiveCommand.CreateAsyncTask(Observable.Return(true), OnRetrySave);
            Setting = SettingsData.Constants.AddressGridSetting;
            LstStores = new ReactiveList<ItemCatalog>();
            HasAdditionalLstStore = Visibility.Collapsed;

            MessageBus.Current.Listen<PropagateOrderModel>(SharedMessageConstants.PROPAGATE_LASTORDER_ADDRESS).Subscribe(OnPropagate);
        }


        private void OnPropagate(PropagateOrderModel model)
        {
            var address = LstAddresses.FirstOrDefault(e => e.AddressInfo.AddressId == model.Order.AddressId);

            if (address == null)
                return;

            RxApp.MainThreadScheduler.Schedule(_ => { AddressSelection = address; });

            if (model.HasEdit)
                return;

            if (model.PosCheck != null)
                MessageBus.Current.SendMessage(model, SharedMessageConstants.PROPAGATE_LASTORDER_POSCHECK);
        }
        protected override void OnShellContainerVmChange(IShellContainerVm value)
        {
            base.OnShellContainerVmChange(value);
            ShellContainerVm.AddOrUpdateFlyouts(_upsertAddress);
        }

        public IReactiveCommand<Unit> Add { get; set; }
        public IReactiveCommand<Unit> Edit { get; set; }
        public IReactiveCommand<Unit> Remove { get; set; }

        public AddressInfoGrid AddressSelection
        {
            get { return _addressSelection; }
            set
            {
                this.RaiseAndSetIfChanged(ref _addressSelection, value);
                ClearAndSelect(_addressSelection);
                OnAddressSelected(_addressSelection);
            }
        }

        public bool IsGettingData
        {
            get { return _isGettingData; }
            set { this.RaiseAndSetIfChanged(ref _isGettingData, value); }
        }

        private void ClearAndSelect(AddressInfoGrid addressSelection)
        {
            foreach (var address in LstAddresses)
            {
                address.IsSelected = false;
            }

            if (addressSelection != null)
                addressSelection.IsSelected = true;
            else if (LstAddresses.Count > 0)
                LstAddresses[0].IsSelected = true;
        }

        public IReactiveCommand<Unit> RetrySaveItem { get; set; }

        public AddressControlSetting Setting { get; set; }


        public Func<ClientFlags.ValidateOrder, ResponseMessage> ValidateModel { get; set; }

        public void SetOrderModel(Func<OrderModel> orderModel)
        {
            _orderModel = orderModel;
        }

        public void OnStoresReceivedByAddress(List<ItemCatalog> obj)
        {
            HasAdditionalLstStore = Visibility.Collapsed;

            if (obj == null || obj.Count < 1)
                return;

            RxApp.MainThreadScheduler.Schedule(_ =>
            {
                LstStores.ClearAndAddRange(obj);
                HasAdditionalLstStore = Visibility.Visible;
                PickUpStore = LstStores[0];  //Always will be the first, because stores are ordering by importance
            });
        }


        public void OnAddressChanged(AddressInfoGrid obj)
        {
            OnAddressChanged(obj, true);
        }

        public void OnAddressChanged(AddressInfoGrid model, bool bHasToSelect)
        {
            if (model != null)
            {
                switch (model.Status)
                {
                    case SharedConstants.Client.RECORD_ERROR_SAVED:
                        model.IsOk = Visibility.Collapsed;
                        model.IsError = Visibility.Visible;
                        model.IsSaveInProgress = Visibility.Collapsed;
                        model.MsgErr = model.Message;
                        break;
                    case SharedConstants.Client.RECORD_ONPROGRESS_TO_SAVED:
                        model.IsOk = Visibility.Collapsed;
                        model.IsError = Visibility.Collapsed;
                        model.IsSaveInProgress = Visibility.Visible;
                        break;
                    case SharedConstants.Client.RECORD_SAVED:
                        model.IsOk = Visibility.Visible;
                        model.IsError = Visibility.Collapsed;
                        model.IsSaveInProgress = Visibility.Collapsed;
                        break;
                }
            }

            if (bHasToSelect)
                RxApp.MainThreadScheduler.Schedule(_ => { AddressSelection = model; });
        }

        public void ProcessPhone(ListItemModel model)
        {

            RxApp.MainThreadScheduler.Schedule(_ =>
            {
                IsGettingData = true;
                LstAddresses.Clear();
            });

            _client.ExecutionProxy.ExecuteRequest<String, String, ResponseMessageData<AddressInfoModel>, ResponseMessageData<AddressInfoModel>>
                (model.Value, TransferDto.SameType, SharedConstants.Server.ADDRESS_HUB,
                    SharedConstants.Server.SEARCH_ADDRESS_BY_PHONE_ADDRESS_HUB_METHOD, TransferDto.SameType)
                .Subscribe(OnAddressListReady, OnAddressListError);
        }


        public event Action<AddressInfoGrid> ItemSelected;

        protected virtual void OnAddressSelected(AddressInfoGrid obj)
        {
            Action<AddressInfoGrid> handler = ItemSelected;
            if (handler != null) handler(obj);
        }

        public event Action<ItemCatalog, bool> ChangeStore;

        protected virtual void OnChangeStore(ItemCatalog obj, bool bIsLastStore)
        {
            var handler = ChangeStore;
            if (handler != null) handler(obj, bIsLastStore);
        }


        private void OnAddressListReady(IStale<ResponseMessageData<AddressInfoModel>> obj)
        {
            if (obj.IsStale)
            {
                OnAddressListError(Resources.Network.ResNetwork.ERROR_NETWORK_DOWN);
                return;
            }

            if (obj.Data.IsSuccess == false)
            {
                OnAddressListError(obj.Data.Message);
                return;
            }

            RxApp.MainThreadScheduler.Schedule(_ =>
            {
                IsGettingData = false;
                LstAddresses.Clear();
                var bIsFirst = true;
                foreach (var info in obj.Data.LstData)
                {
                    var row = new AddressInfoGrid
                    {
                        AddressInfo = info,
                        Status = SharedConstants.Client.RECORD_SAVED,
                        IsSelected = bIsFirst
                    };

                    OnAddressChanged(row, bIsFirst);
                    LstAddresses.Add(row);
                    bIsFirst = false;
                }

                if (bIsFirst)
                    OnAddressChanged(null, true);

            });
        }

        private void OnAddressListError(Exception ex)
        {
            OnAddressListError(ex.Message);
        }

        private void OnAddressListError(string msgErr)
        {
            RxApp.MainThreadScheduler.Schedule(_ =>
            {
                IsGettingData = false;
            });

            MessageBus.Current.SendMessage(new MessageBoxSettings
            {
                Message = msgErr,
                Title = "Cargando la(s) dirección(es)",
            }, SharedMessageConstants.MSG_SHOW_ERRQST);
        }

        public IReactiveList<AddressInfoGrid> LstAddresses
        {
            get
            {
                var orderModel = _orderModel();
                return orderModel == null ? null : orderModel.LstAddressInfo;
            }
        }

        public ReactiveList<ItemCatalog> LstStores { get; set; }

        public ItemCatalog PickUpStore
        {
            get { return _pickUpStore; }
            set
            {
                this.RaiseAndSetIfChanged(ref _pickUpStore, value);
                if (_pickUpStore != null)
                    OnChangeStore(_pickUpStore, true);

            }
        }

        public Visibility HasAdditionalLstStore
        {
            get { return _hasAdditionalLstStore; }
            set
            {
                this.RaiseAndSetIfChanged(ref _hasAdditionalLstStore, value);
            }
        }

        public IUpsertAddressFoVm UpsertAddress
        {
            get { return _upsertAddress; }
            set { this.RaiseAndSetIfChanged(ref _upsertAddress, value); }
        }

        public async Task<Unit> OnAdd()
        {
            await Task.Run(() =>
            {
                try
                {
                    var response = ValidateModel(ClientFlags.ValidateOrder.Phone | ClientFlags.ValidateOrder.Franchise);
                    if (response.IsSuccess == false)
                    {
                        MessageBus.Current.SendMessage(new MessageBoxSettings
                        {
                            Message = response.Message,
                            Title = "Información faltante",
                        }, SharedMessageConstants.MSG_SHOW_ERRQST);
                        return;
                    }
                    UpsertAddress.Clean(_orderModel().Franchise);
                    UpsertAddress.IsOpen = true;

                }
                catch (Exception ex)
                {
                    InternalLogger.WriteException(ex);
                    MessageBus.Current.SendMessage(new MessageBoxSettings
                    {
                        Message = ex.Message,
                        Title = "Agregar dirección",
                    }, SharedMessageConstants.MSG_SHOW_ERRQST);
                }
            });
            return new Unit();
        }

        public async Task<Unit> OnEdit(object oRow)
        {
            await Task.Run(() =>
            {
                try
                {

                    var clInfo = oRow as AddressInfoGrid;
                    if (clInfo == null)
                        return;

                    UpsertAddress.Fill(clInfo, _orderModel().Franchise);
                    UpsertAddress.IsOpen = true;
                }
                catch (Exception ex)
                {
                    InternalLogger.WriteException(ex);
                    MessageBus.Current.SendMessage(new MessageBoxSettings
                    {
                        Message = ex.Message,
                        Title = "Editar dirección",
                    }, SharedMessageConstants.MSG_SHOW_ERRQST);
                }
            });
            return new Unit();
        }


        private async Task<Unit> OnRetrySave(object o)
        {
            var lstItemSelected = o as ObservableCollection<object>;

            if (lstItemSelected != null && lstItemSelected.Count > 0)
            {
                var model = lstItemSelected[0] as AddressInfoGrid;
                if (model != null)
                    await Task.Run(() => MessageBus.Current.SendMessage(model, SharedMessageConstants.ORDER_CLIENTINFO));
            }

            return new Unit();
        }

        public async Task<Unit> OnRemove(object oRow)
        {
            await Task.Run(() =>
            {
                var clInfo = oRow as AddressInfoGrid;
                if (clInfo == null || clInfo.AddressInfo == null || clInfo.AddressInfo.PrimaryPhone == null || clInfo.AddressInfo.AddressId == null)
                    return;

                MessageBus.Current.SendMessage(new MessageBoxSettings
                {
                    Message = "¿Desea eliminar este dirección de la lista?",
                    Title = "Eliminar el dirección",
                    Settings = new MetroDialogSettings
                    {
                        AffirmativeButtonText = "eliminar",
                        NegativeButtonText = "cancelar"
                    },
                    Style = MessageDialogStyle.AffirmativeAndNegative,
                    Callback = (x => DoRemoveAddress(x, clInfo))

                }, SharedMessageConstants.MSG_SHOW_ERRQST);

            });
            return new Unit();
        }

        private void DoRemoveAddress(MessageDialogResult result, AddressInfoGrid clInfo)
        {
            if (result != MessageDialogResult.Affirmative)
                return;

            var relClientPhone = new AddressPhoneModel
            {
                AddressId = clInfo.AddressInfo.AddressId ?? SharedConstants.NULL_ID_VALUE,
                AddressPhoneId = clInfo.AddressInfo.PrimaryPhone.PhoneId
            };

            _client.ExecutionProxy
                .ExecuteRequest<AddressPhoneModel, AddressPhoneModel, ResponseMessageData<bool>, ResponseMessageData<bool>>
                (relClientPhone, TransferDto.SameType, SharedConstants.Server.ADDRESS_HUB,
                    SharedConstants.Server.REMOVE_REL_PHONECLIENT_ADDRESS_HUB_METHOD, TransferDto.SameType)
                .Subscribe(x => OnRemoveDone(x, clInfo), OnRemoveError);

        }

        private void OnRemoveError(Exception ex)
        {
            OnRemoveError(ex.Message);
        }

        private void OnRemoveError(String msgError)
        {
            MessageBus.Current.SendMessage(new MessageBoxSettings
            {
                Message = String.Format("No fue posible eliminar el cliente debido a: {0}", msgError),
                Title = "Eliminar el cliente"
            }, SharedMessageConstants.MSG_SHOW_ERRQST);
        }

        private void OnRemoveDone(IStale<ResponseMessageData<bool>> obj, AddressInfoGrid clInfo)
        {
            if (obj.IsStale)
            {
                OnRemoveError(Resources.Network.ResNetwork.ERROR_NETWORK_DOWN);
                return;
            }

            if (obj.Data.IsSuccess == false)
            {
                OnRemoveError(obj.Data.Message);
                return;
            }

            RxApp.MainThreadScheduler.Schedule(_ =>
            {
                var client = LstAddresses.FirstOrDefault(e => e.PreId == clInfo.PreId);
                if (client != null)
                    LstAddresses.Remove(client);
            });
        }
    }
}
