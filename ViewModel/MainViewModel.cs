using ChannelAdvisor_Api.Helpers;
using ChannelAdvisor_Api.Models;
using ChannelAdvisor_Api.OrderService;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChannelAdvisor_Api.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private RelayCommand _callChannelAdvisorCommand;
        private Logger _logger;
        private RelayCommand _callAuthorizationListCommand;

        public RelayCommand CallChannelAdvisorCommand
        {
            get
            {
                return _callChannelAdvisorCommand ?? (_callChannelAdvisorCommand = new RelayCommand(
                           () =>
                           {
                               ExecuteCallChannelAdivosrApiCommand();
                           }));
            }
        }
        public RelayCommand CallAuthorizationListCommand
        {
            get
            {
                return _callAuthorizationListCommand ?? (_callAuthorizationListCommand = new RelayCommand(
                           () =>
                           {
                               ExecuteCallAuthorizationListCommand();
                           }));
            }
        }

        private string _developerKey = "";
        public string DeveloperKey
        {
            get => _developerKey;
            set
            {
                _developerKey = value;
                RaisePropertyChanged();
            }
        }

        private string _developerPassword = "";
        public string DeveloperPassword
        {
            get => _developerPassword;
            set
            {
                _developerPassword = value;
                RaisePropertyChanged();
            }
        }

        private string _shippingStatus = "Unshipped";
        public string ShippingStatus
        {
            get => _shippingStatus;
            set
            {
                _shippingStatus = value;
                RaisePropertyChanged();
            }
        }

        private string _paymentStatus = "Cleared";
        public string PaymentStatus
        {
            get => _paymentStatus;
            set
            {
                _paymentStatus = value;
                RaisePropertyChanged();
            }
        }

        private string _profiles;
        public string Profiles
        {
            get => _profiles;
            set
            {
                _profiles = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<ChannelAdvisorAccount> _profileList = new ObservableCollection<ChannelAdvisorAccount>();
        public ObservableCollection<ChannelAdvisorAccount> ProfileList
        {
            get => _profileList;
            set
            {
                _profileList = value;
                RaisePropertyChanged();
            }
        }

        private ChannelAdvisorAccount _selectedProfile;
        public ChannelAdvisorAccount SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                _selectedProfile = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<ChannelAdvisorOrder> _orderList = new ObservableCollection<ChannelAdvisorOrder>();

        public ObservableCollection<ChannelAdvisorOrder> OrderList
        {
            get => _orderList;
            set
            {
                _orderList = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            _logger = new LoggerConfiguration().WriteTo.RollingFile("d:\\temp\\ChannelAdvisor-{Date}.txt").CreateLogger();
        }

        private string BuildContactName(string title, string firstName, string lastName)
        {
            return title + " " + firstName + " " + lastName;
        }

        private string GetTotalContent(OrderResponseDetailComplete order)
        {
            var result = "";
            if ((order.ShoppingCart.LineItemSKUList != null) && (order.ShoppingCart.LineItemSKUList.Length > 0))
            {

                foreach (OrderLineItemItemResponse item in order.ShoppingCart.LineItemSKUList)
                {
                    if (result == "")
                    {
                        result = item.Title;
                    }
                    else
                    {
                        result = result + " " + item.Title;
                    }
                }
            }
            return result;
        }

        private ChannelAdvisorAccount Create(string status, string account, int localId)
        {
            return new ChannelAdvisorAccount() { AccountId = account, Status = status, LocalId = localId };
        }

        public void ExecuteCallAuthorizationListCommand()
        {
            var accountProfiles = Profiles.Split(',');
            var notOkProfiles = new List<string>();
            var adminService = new AdminService.AdminServiceSoapClient();
            adminService.Endpoint.EndpointBehaviors.Add(new ChannelAdvisorLogger(_logger));

            var apiCredentials =
                new AdminService.APICredentials
                {
                    DeveloperKey = DeveloperKey,
                    Password = DeveloperPassword
                };
            var response = adminService.GetAuthorizationList(apiCredentials, "");
            _logger.Information("Got response from salesOrderList method. Result: {1},{2}", response.Status, response.Message);

            if ((response.Status != AdminService.ResultStatus.Success) || (!response.ResultData.Any()))
                ;
            else
            {
                foreach (var connectorProfile in accountProfiles)
                {
                    var ok = false;
                    foreach (var channelProfile in response.ResultData)
                    {
                        if (connectorProfile == channelProfile.LocalID.ToString())
                        {
                            ok = true;
                            ProfileList.Add(Create(channelProfile.Status, channelProfile.AccountID,
                                    channelProfile.LocalID));
                        }
                    }
                    if (!ok)
                        notOkProfiles.Add(connectorProfile);
                }
                foreach (var request in notOkProfiles)
                {
                    _logger.Information("Requesting access to profile {1}", request);

                    var profileId = int.Parse(request);
                    var requestAccessResponse = adminService.RequestAccess(apiCredentials, profileId);
                    if (requestAccessResponse.Status != AdminService.ResultStatus.Success)
                    {
                        _logger.Error("Failed response {1},{2}", requestAccessResponse.Status,
                            requestAccessResponse.Message);
                    }
                    else
                    {
                        _logger.Information("Got response {1},{2}", requestAccessResponse.Status,
                            requestAccessResponse.Message);
                    }
                }
            }
        }

        public void ExecuteCallChannelAdivosrApiCommand()
        {
            if (SelectedProfile != null)
            {
                var orderService = new OrderService.OrderServiceSoapClient();
                orderService.Endpoint.EndpointBehaviors.Add(new ChannelAdvisorLogger(_logger));
                var apiCredentials = new OrderService.APICredentials()
                {
                    DeveloperKey = DeveloperKey,
                    Password = DeveloperPassword
                };
                var orderCriteria = new OrderCriteria
                {
                    ShippingStatusFilter = ShippingStatus,
                    PaymentStatusFilter = PaymentStatus,
                    DetailLevel = "Complete",
                    PageSize = 20,
                    ExportState = "NotExported",
                    PageNumberFilter = 0
                };

                var moreResults = true;
                while (moreResults)
                {
                    orderCriteria.PageNumberFilter += 1;
                    var accountGuid = SelectedProfile.AccountId;
                    var response = orderService.GetOrderList(apiCredentials, accountGuid, orderCriteria);
                    if (response.Status != OrderService.ResultStatus.Success)
                    {
                        _logger.Error("Connection Failed response {1},{2}", response.Status, response.Message);
                    }
                    else
                    {
                        _logger.Information("Got response {1},{2}, count{3}", response.Status, response.Message,
                            response.ResultData.Length);
                        var orderList = response.ResultData;
                        moreResults = !((orderList.Length == 0) || (orderList.Length < orderCriteria.PageSize));

                        foreach (OrderResponseDetailComplete order in orderList)
                        {
                            var orderInformation = new ChannelAdvisorOrder()
                            {
                                OrderId = order.OrderID.ToString(),
                                Phone = string.IsNullOrEmpty(order.ShippingInfo.PhoneNumberDay)
                                    ? order.ShippingInfo.PhoneNumberEvening
                                    : order.ShippingInfo.PhoneNumberDay,
                                ContactName = BuildContactName(order.ShippingInfo.JobTitle,
                                    order.ShippingInfo.FirstName, order.ShippingInfo.LastName),
                                Company = order.ShippingInfo.CompanyName,
                                Street = order.ShippingInfo.AddressLine1,
                                District = order.ShippingInfo.AddressLine2,
                                CityTown = order.ShippingInfo.City,
                                County = order.ShippingInfo.Region,
                                PostCode = order.ShippingInfo.PostalCode,
                                CountryCode = order.ShippingInfo.CountryCode,
                                ShipReference = order.ClientOrderIdentifier,
                                Instructions = order.ShippingInfo.ShippingInstructions,
                                Email = order.BuyerEmailAddress,
                                Value = order.TotalOrderAmount,
                                Content = GetTotalContent(order),
                                LineItems = new List<ChannelAdvisorLineItem>()
                            };

                            foreach (OrderLineItemItemResponse item in order.ShoppingCart.LineItemSKUList)
                            {
                                var lineItem = new ChannelAdvisorLineItem()
                                {
                                    Weight = Convert.ToDecimal(item.UnitWeight.Value * item.Quantity),
                                    Content = item.Title,
                                    Value = item.UnitPrice * item.Quantity,
                                    ShipReference = item.SKU,
                                    WarehouseLocation = item.WarehouseLocation
                                };

                                orderInformation.LineItems.Add(lineItem);
                            }
                            OrderList.Add(orderInformation);
                        }
                    }
                }
            }
        }

    }
}