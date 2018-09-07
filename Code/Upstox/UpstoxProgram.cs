﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using StockTrader.Brokers.UpstoxBroker;
using StockTrader.Core;
using StockTrader.Platform.Logging;
using StockTrader.Utilities;
using StockTrader.Utilities.Broker;
using UpstoxNet;

namespace SimpleTrader
{
    class UpstoxProgram
    {
        private static Mutex mutex;

        static UpstoxProgram()
        {
            var filesPath = SystemUtils.GetStockFilesPath();
            string credsFilePath = Path.Combine(filesPath, "creds.txt");
            var credsLines = File.ReadAllLines(credsFilePath);

            userId = credsLines[0];
            apiKey = credsLines[1];
            apiSecret = credsLines[2];
            redirectUrl = credsLines[3];

            mutex = new Mutex(false, userId);
        }

        //Run logs - C:\StockRunFiles\TraderLogs\
        static double _pctLtpOfLastBuyPriceForAveraging = 0.99;
        static double _buyMarkdownFromLtpDefault = 0.985;
        static double _sellMarkupForMargin = 1.015;
        static double _sellMarkupForDelivery = 1.025;   // 2.5%
        static double _sellMarkupForMinProfit = 1.0035; // .35%
        static double _sellMarkupForEODInsufficientLimitSquareOff = 0.995;
        static double _pctSquareOffForMinProfit = 0.0035;
        static bool _squareOffAllPositionsAtEOD = false;
        static double _pctMaxLossSquareOffPositionsAtEOD = .002; // 0.5%
        static bool _useAvgBuyPriceInsteadOfLastBuyPriceToCalculateBuyPriceForNewOrder = false;
        static bool _doConvertToDeliveryAtEOD = true;
        static bool _doSquareOffIfInsufficientLimitAtEOD = false;
        static DateTime _startTime = MarketUtils.GetTimeToday(9, 0);
        static DateTime _endTime = MarketUtils.GetTimeToday(15, 00);
        private static string userId;
        private static string apiKey;
        private static string apiSecret;
        private static string redirectUrl;


        [STAThread]
        static void Main(string[] args)
        {
            BrokerErrorCode errCode = BrokerErrorCode.Unknown;

            var upstoxBroker = new MyUpstoxWrapper(apiKey, apiSecret, redirectUrl);
            errCode = upstoxBroker.Login();

            // Check for Holiday today
            if (IsHolidayToday())
            {
                Trace("Exchange Holiday today.. exiting.");
                return;
            }

            // Wait 2 seconds if contended – in case another instance
            // of the program is in the process of shutting down.
            if (!mutex.WaitOne(TimeSpan.FromSeconds(2), false))
            {
                var message = "Another instance of the app is running. Only one instance for a userId is allowed! UserId: " + userId + ". Exiting..";
                Trace(message);
                //Console.WriteLine("Press any key to exit..");
                //Console.ReadKey();
                return;
            }

            Trace(userId);
            MarketUtils.WaitUntilMarketOpen();

            //while (true)
           //     Thread.Sleep(1000);

            //// Separate Login thread in background
            //BrokingAccountThread loginThread = new BrokingAccountThread();
            //Thread thread = new Thread(new ParameterizedThreadStart(LoginUtils.Background_LoginCheckerThread));
            //BrokingAccountObject iciciAccObj = new BrokingAccountObject(upstox);
            //loginThread.brokerAccountObj = iciciAccObj;

            //loginThread.thread = thread;
            //loginThread.thread.Name = "Main Login Thread of MUNISGcS";
            //loginThread.thread.IsBackground = true;
            //loginThread.thread.Start(iciciAccObj);

            // ************ TESTING Code starts ****************** //
            //Trace("Testing starts");
            //string ordertestref = "";aswwww345678899990
            //double funds;
            //errCode = upstox.GetFundsAvailable(out funds);
            //errCode = upstox.AllocateFunds(FundAllocationCategory.IpoMf, funds);
            //errCode = upstox.AllocateFunds(FundAllocationCategory.IpoMf, -4.67);
            //var str = string.Format("[Holding EOD] {0} {1}", OrderPositionTypeEnum.Btst, "");
            //errCode = CancelEquityOrder(string.Format("[Holding EOD] {0} {1}", HoldingTypeEnum.Btst, ""), ref ordertestref, OrderDirection.SELL);
            //var newTrades = new Dictionary<string, EquityTradeBookRecord>();
            //List<EquityDematHoldingRecord> holdings;
            //List<EquityBTSTTradeBookRecord> btstHoldings;
            //errCode = upstox.GetBTSTListings("CAPFIR", out btstHoldings);
            //List<EquityPendingPositionForDelivery> pendingPositions;
            //errCode = upstox.GetOpenPositionsPendingForDelivery("BAJFI", out pendingPositions);
            //errCode = upstox.GetOpenPositionsPendingForDelivery("MOTSUM", out pendingPositions);
            //Dictionary<string, EquityOrderBookRecord> orders;
            //errCode = upstox.GetEquityOrderBookToday(false, false, "MOTSUM", out orders);
            //errCode = upstox.PlaceEquityMarginSquareOffOrder("BAJFI", 34, 34, "1733", OrderPriceType.LIMIT, OrderDirection.SELL, EquityOrderType.DELIVERY, "2017232", Exchange.NSE, out ordertestref);
            //errCode = upstox.PlaceEquityMarginDeliveryFBSOrder("BAJFI", 5, "1733", OrderPriceType.LIMIT, OrderDirection.SELL, EquityOrderType.DELIVERY, Exchange.NSE, out ordertestref);

            //errCode = upstox.GetDematAllocation("BAJFI", out holdings);
            //errCode = upstox.GetDematAllocation("CAPFIR", out holdings);
            //errCode = upstox.GetDematAllocation("CAPPOI", out holdings);
            //errCode = upstox.ConvertToDeliveryFromMarginOpenPositions("BAJFI", 3, 1, "2017223", OrderDirection.BUY, Exchange.NSE);
            //errCode = upstox.GetEquityTradeBookToday(true, "BAJFI", out newTrades);
            //errCode = upstox.ConvertToDeliveryFromPendingForDelivery("CAPFIR", 4, 1, "2017217", Exchange.NSE);
            //errCode = upstox.CancelEquityOrder("20171120N900017136", EquityOrderType.DELIVERY);
            //while (errCode != BrokerErrorCode.Success)
            //{
            //    errCode = upstox.CancelAllOutstandingEquityOrders("BAJFI", out Dictionary<string, BrokerErrorCode> cancelledOrders);
            //    errCode = upstox.PlaceEquityDeliveryBTSTOrder("BAJFI", 1, "1831", OrderPriceType.LIMIT, OrderDirection.SELL, EquityOrderType.DELIVERY, Exchange.NSE, "2017221", out ordertestref);
            //}
            ////errCode = upstox.PlaceEquityMarginDeliveryFBSOrder("BAJFI", 1, "1831", OrderPriceType.LIMIT, OrderDirection.SELL, EquityOrderType.DELIVERY, Exchange.NSE, out ordertestref);
            //Trace("Testing ends");
            // ************ TESTING Code ends ****************** //

            // Read the config file
            List <UpstoxTradeParams> stocksConfig = ReadTradingConfigFile();

            var threads = new List<Thread>(stocksConfig.Count);

            foreach (var stockConfig in stocksConfig)
            {
                stockConfig.upstox = upstoxBroker;
                var t = new Thread(new UpstoxAverageTheBuyThenSell(stockConfig).StockBuySell);
                threads.Add(t);
            }

            threads.ForEach(t => { t.Start(); Thread.Sleep(1000); });
            threads.ForEach(t => t.Join());

            // Send out the log in email and chat
            Trace("All stock threads completed. Emailing today's log file");
            MessagingUtils.Init();
            var log = GetLogContent();
            MessagingUtils.SendAlertMessage("SimpleTrader log", log);
            Trace("Exiting..");
        }

        public static bool IsHolidayToday()
        {
            // Read up holidays list
            var filesPath = SystemUtils.GetStockFilesPath();
            string holidaysFilePath = Path.Combine(filesPath, "holidayslist.txt");
            var holidayDateStrs = File.ReadAllLines(holidaysFilePath);

            var holidayDates = holidayDateStrs.Select(h => DateTime.Parse(h));

            return holidayDates.Any(hd => hd.Date == DateTime.Today.Date);
        }

        public static List<UpstoxTradeParams> ReadTradingConfigFile()
        {
            var filesPath = SystemUtils.GetStockFilesPath();

            string configFilePath = Path.Combine(filesPath, "stocktradingconfig.txt");

            var lines = File.ReadAllLines(configFilePath);
            var common = lines[1].Split(',');

            List<UpstoxTradeParams> tps = new List<UpstoxTradeParams>(lines.Length);

            int Index = -1;
            var ctp = new UpstoxTradeParams
            {
                stockCode = "COMMONCONFIG",
                isinCode = "",
                maxTradeValue = double.Parse(common[++Index]),
                maxTotalPositionValueMultiple = int.Parse(common[++Index]),
                maxTodayPositionValueMultiple = int.Parse(common[++Index]),
                pctExtraMarkdownForAveraging = double.Parse(common[++Index]),
                buyMarkdownFromLcpDefault = double.Parse(common[++Index]),
                sellMarkupForMargin = double.Parse(common[++Index]),
                sellMarkupForDelivery = double.Parse(common[++Index]),
                sellMarkupForMinProfit = double.Parse(common[++Index]),
                pctSquareOffForMinProfit = double.Parse(common[++Index]),
                squareOffAllPositionsAtEOD = bool.Parse(common[++Index]),
                pctMaxLossSquareOffPositionsAtEOD = double.Parse(common[++Index]),
                useAvgBuyPriceInsteadOfLastBuyPriceToCalculateBuyPriceForNewOrder = bool.Parse(common[++Index]),
                startTime = GeneralUtils.GetTodayDateTime(common[++Index]),
                endTime = GeneralUtils.GetTodayDateTime(common[++Index]),
                exchange = (Exchange)Enum.Parse(typeof(Exchange), common[++Index]),
                sellMarkupForEODInsufficientLimitSquareOff = double.Parse(common[++Index]),
                maxBuyOrdersAllowedInADay = int.Parse(common[++Index]),
                doConvertToDeliveryAtEOD = bool.Parse(common[++Index]),
                doSquareOffIfInsufficientLimitAtEOD = bool.Parse(common[++Index]),
                orderType = (EquityOrderType)Enum.Parse(typeof(EquityOrderType), common[++Index])
            };

            for (int i = 2; i < lines.Length; i++)
            {
                Index = -1;
                var stock = lines[i].Split(',');
                var stockCode = stock[++Index];//0
                if (stockCode.Trim().StartsWith("#"))
                    continue;

                var goodPrice = double.Parse(stock[++Index]);//1
                var buyPriceCap = double.Parse(stock[++Index]);//2
                var orderType = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.orderType : (EquityOrderType)Enum.Parse(typeof(EquityOrderType), stock[Index])) : ctp.orderType;//22
                var maxTradeVal = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.maxTradeValue : double.Parse(stock[Index])) : ctp.maxTradeValue;//3
                var ordQty = (int)Math.Round(maxTradeVal / goodPrice);
                var maxTotalPositionValueMultiple = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.maxTotalPositionValueMultiple : int.Parse(stock[Index])) : ctp.maxTotalPositionValueMultiple;//4
                var maxTodayPositionValueMultiple = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.maxTodayPositionValueMultiple : int.Parse(stock[Index])) : ctp.maxTodayPositionValueMultiple;//5

                var tp = new UpstoxTradeParams
                {
                    stockCode = stockCode,
                    isinCode = "",
                    maxTradeValue = maxTradeVal,
                    maxTotalPositionValueMultiple = maxTotalPositionValueMultiple,
                    maxTodayPositionValueMultiple = maxTodayPositionValueMultiple,
                    orderType = orderType,
                    ordQty = ordQty,
                    maxTotalOutstandingQtyAllowed = ordQty * maxTotalPositionValueMultiple,
                    maxTodayOutstandingQtyAllowed = ordQty * maxTodayPositionValueMultiple,
                    goodPrice = goodPrice,
                    buyPriceCap = buyPriceCap,
                    pctExtraMarkdownForAveraging = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.pctExtraMarkdownForAveraging : double.Parse(stock[Index])) : ctp.pctExtraMarkdownForAveraging,//6
                    buyMarkdownFromLcpDefault = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.buyMarkdownFromLcpDefault : double.Parse(stock[Index])) : ctp.buyMarkdownFromLcpDefault,//7
                    sellMarkupForMargin = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.sellMarkupForMargin : double.Parse(stock[Index])) : ctp.sellMarkupForMargin,//8
                    sellMarkupForDelivery = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.sellMarkupForDelivery : double.Parse(stock[Index])) : ctp.sellMarkupForDelivery,//9
                    sellMarkupForMinProfit = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.sellMarkupForMinProfit : double.Parse(stock[Index])) : ctp.sellMarkupForMinProfit,//10
                    pctSquareOffForMinProfit = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.pctSquareOffForMinProfit : double.Parse(stock[Index])) : ctp.pctSquareOffForMinProfit,//11
                    squareOffAllPositionsAtEOD = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.squareOffAllPositionsAtEOD : bool.Parse(stock[Index])) : ctp.squareOffAllPositionsAtEOD,//12
                    pctMaxLossSquareOffPositionsAtEOD = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.pctMaxLossSquareOffPositionsAtEOD : double.Parse(stock[Index])) : ctp.pctMaxLossSquareOffPositionsAtEOD,//13
                    useAvgBuyPriceInsteadOfLastBuyPriceToCalculateBuyPriceForNewOrder = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.useAvgBuyPriceInsteadOfLastBuyPriceToCalculateBuyPriceForNewOrder : bool.Parse(stock[Index])) : ctp.useAvgBuyPriceInsteadOfLastBuyPriceToCalculateBuyPriceForNewOrder,//14
                    startTime = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.startTime : GeneralUtils.GetTodayDateTime(stock[Index])) : ctp.startTime,//15
                    endTime = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.endTime : GeneralUtils.GetTodayDateTime(stock[Index])) : ctp.endTime,//16
                    exchange = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.exchange : (Exchange)Enum.Parse(typeof(Exchange), stock[Index])) : ctp.exchange,//17
                    sellMarkupForEODInsufficientLimitSquareOff = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.sellMarkupForEODInsufficientLimitSquareOff : double.Parse(stock[Index])) : ctp.sellMarkupForEODInsufficientLimitSquareOff,//18
                    maxBuyOrdersAllowedInADay = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.maxBuyOrdersAllowedInADay : int.Parse(stock[Index])) : ctp.maxBuyOrdersAllowedInADay,//19
                    doConvertToDeliveryAtEOD = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.doConvertToDeliveryAtEOD : bool.Parse(stock[Index])) : ctp.doConvertToDeliveryAtEOD,//20
                    doSquareOffIfInsufficientLimitAtEOD = stock.Length > ++Index ? (string.IsNullOrEmpty(stock[Index]) ? ctp.doSquareOffIfInsufficientLimitAtEOD : bool.Parse(stock[Index])) : ctp.doSquareOffIfInsufficientLimitAtEOD//21
                };

                tps.Add(tp);
            }

            return tps;
        }

        public static void Trace(string inMessage)
        {
            var consoleMessage = DateTime.Now.ToString() + " " + inMessage;
            Console.WriteLine(consoleMessage);
            FileTracing.TraceOut(inMessage);
        }

        public static string GetLogContent()
        {
            FileStream logFileStream = new FileStream(FileTracing.GetTraceFilename(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader logFileReader = new StreamReader(logFileStream);
            string logContent = logFileReader.ReadToEnd();

            logFileReader.Close();
            logFileStream.Close();

            return logContent;
        }
    }

    public class UpstoxTradeParams
    {
        public MyUpstoxWrapper upstox;
        public string stockCode;
        public string isinCode;
        public double maxTradeValue = 50000;
        public int maxTotalPositionValueMultiple = 4;
        public int maxTodayPositionValueMultiple = 2;
        public int ordQty;
        public int maxTotalOutstandingQtyAllowed;
        public int maxTodayOutstandingQtyAllowed;
        public int maxBuyOrdersAllowedInADay = 4;
        public Exchange exchange;

        // default
        public double buyPriceCap;
        public double goodPrice;
        public double pctExtraMarkdownForAveraging = 0.99;
        public double buyMarkdownFromLcpDefault = 0.99;
        public double sellMarkupForMargin = 1.02;
        public double sellMarkupForDelivery = 1.025;
        public double sellMarkupForMinProfit = 1.0035;
        public double sellMarkupForEODInsufficientLimitSquareOff = 0.995;
        public double pctSquareOffForMinProfit = 0.0035;
        public bool squareOffAllPositionsAtEOD = false;
        public double pctMaxLossSquareOffPositionsAtEOD = .01;
        public bool useAvgBuyPriceInsteadOfLastBuyPriceToCalculateBuyPriceForNewOrder = false;
        public bool doConvertToDeliveryAtEOD = true;
        public bool doSquareOffIfInsufficientLimitAtEOD = false;
        public EquityOrderType orderType = EquityOrderType.MARGIN;

        public DateTime startTime = MarketUtils.GetTimeToday(9, 0);
        public DateTime endTime = MarketUtils.GetTimeToday(15, 0);
    }
}
