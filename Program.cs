
using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace SimulationAgentBasedModel
{
    /*
    * We have groups of teller windows, so different types
    * of teller windows handle different transactions
    *
    * For example a client that wants to open an account
    * can only do it on window #1 and so on
    *
    */
    struct GroupThread
    {
        public int ID { get; set; }
        public List<Thread> threadsList { get; set; }
    }

    class Program
    {
        private static int groups;
        private static int queueSize;
        private static List<GroupThread> threads;
        private static Thread producerThread;
        private static Mutex mutex = new Mutex();
        private static Random rand = new Random();
        private static ConcurrentQueue<Tuple<int, int, int>> queue;

        [MTAThread]
        public static void Main()
        {
            int consumers;

            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

            do
            {
                Console.WriteLine("Enter the number of groups of windows -operators- (n > 0)");
                if (Int32.TryParse(Console.ReadLine(), out groups) && groups > 0)
                {
                    do
                    {

                        Console.WriteLine("Enter the max number of customers for each window -visitors- (n > 0)");
                        if (Int32.TryParse(Console.ReadLine(), out consumers) && consumers > 0)
                        {
                            do
                            {
                                Console.WriteLine("Enter the number of people (n > 4)");
                                if (Int32.TryParse(Console.ReadLine(), out queueSize) && queueSize > 4)
                                {
                                    threads = new List<GroupThread>(groups);
                                    queue = new ConcurrentQueue<Tuple<int, int, int>>();

                                    for (int i = 0; i < queueSize; i++)
                                    {
                                        queue.Enqueue(Tuple.Create(rand.Next(0, groups), rand.Next(), rand.Next()));
                                    }

                                    producerThread = new Thread(new ThreadStart(ProducerThreadProc));
                                    producerThread.Start();

                                    for (int i = 1; i <= groups; i++)
                                    {
                                        GroupThread groupThread = new GroupThread
                                        {
                                            ID = i,
                                            threadsList = new List<Thread>(rand.Next(1, consumers))
                                        };

                                        for (int j = 0; j < groupThread.threadsList.Capacity; j++)
                                        {
                                            Thread ct = new Thread(new ParameterizedThreadStart(ConsumerThreadProc));
                                            ct.Start(groupThread);
                                            groupThread.threadsList.Add(ct);
                                        }
                                        threads.Add(groupThread);
                                    }

                                    //Infinite number of people arrives at the bank.
                                    while (true)
                                    {
                                        //Current number of people waiting at the bank
                                        Console.WriteLine("Waiting in line: {0}", queue.Count);

                                        //Operators attending people on their windows
                                        Console.WriteLine("State of the Windows");
                                        for (int i = 0; i < threads.Count; i++)
                                        {
                                            Console.WriteLine("Group: {0}", i + 1);
                                            for (int j = 1; j <= threads[i].threadsList.Count; j++)
                                            {
                                                Console.WriteLine("- {0} -> {1}", j, threads[i].threadsList[j - 1].ThreadState);
                                            }
                                            Console.WriteLine();
                                        }

                                        Thread.Sleep(1000);
                                    }
                                }
                                else
                                {
                                    queueSize = 0;
                                }
                            } while (queueSize > 4);
                        }
                        else
                        {
                            consumers = 0;
                        }
                    } while (consumers > 0);
                }
                else
                {
                    groups = 0;
                }
            } while (groups > 0);
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                producerThread.Interrupt();

                foreach (GroupThread currentGroup in threads)
                {
                    foreach (Thread current in currentGroup.threadsList)
                    {
                        current.Interrupt();
                    }
                }
                //mutex.Dispose();
            }
        }

        /// <summary>
        /// Handles the queue of the bank, to let clients go to their respective operator
        /// Within the tuple, a new client is enqueued with a type of task and priority
        /// </summary>
        public static void ProducerThreadProc()
        {
            try
            {
                while (true)
                {
                    if (queue.Count < queueSize)
                    {
                        queue.Enqueue(Tuple.Create(rand.Next(0, groups), rand.Next(), rand.Next()));
                    }

                    Thread.Sleep(4000);
                    Thread.Yield();
                }
            }
            catch (ThreadInterruptedException) { return; }
        }

        /// <summary>
        /// Here is where the operator work happens
        /// From people waiting, we pick the next
        /// and check if the free window can do the work
        /// So different types of clients can only go
        /// to some group of teller windows
        /// </summary>
        /// <param name="data"></param>
        public static void ConsumerThreadProc(object data)
        {
            var currentData = (GroupThread)data;

            try
            {
                while (true)
                {
                    if (queue.Count > 0)
                    {
                        if (mutex.WaitOne())
                        {
                            if (queue.TryPeek(out Tuple<int, int, int> m) && m.Item1 == currentData.ID)
                            {
                                if (queue.TryDequeue(out Tuple<int, int, int> n))
                                {
                                    mutex.ReleaseMutex();

                                    //Constant Time-Step of 5s
                                    //Each process takes the same time for simplicity
                                    Thread.Sleep(5000);
                                }
                            }
                        }
                    }
                    Thread.Yield();
                }
            }
            catch (ThreadInterruptedException) { return; }
        }
    }
}
