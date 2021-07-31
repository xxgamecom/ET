﻿using System;
using System.Threading;
using CommandLine;
using ET.EventType;
using NLog;

namespace ET
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => { Log.Error(e.ExceptionObject.ToString()); };

            // 异步方法全部会回掉到主线程
            SynchronizationContext.SetSynchronizationContext(ThreadSynchronizationContext.Instance);

            try
            {
                Game.EventSystem.Add(typeof(Game).Assembly);
                Game.EventSystem.Add(DllHelper.GetHotfixAssembly());

                ProtobufHelper.Init();
                MongoHelper.Init();

                // 命令行参数
                Options options = null;
                Parser.Default
                        .ParseArguments<Options>(args)
                        .WithNotParsed(error => throw new Exception($"命令行格式错误!"))
                        .WithParsed(o => { options = o; });

                Game.Options = options;

                // logger
                Game.ILog = new NLogger(Game.Options.AppType.ToString());
                LogManager.Configuration.Variables["appIdFormat"] = $"{Game.Options.Process:000000}";

                Log.Info($"server start........................ {Game.Scene.Id}");

                // 启动
                Game.EventSystem.Publish(new AppStart());

                while (true)
                {
                    try
                    {
                        Thread.Sleep(1);
                        Game.Update();
                        Game.LateUpdate();
                        Game.FrameFinish();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}