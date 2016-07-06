// Copyright (c) 2016 Hylke Bons
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.


using System;
using System.Threading;

using NetMQ;
using NetMQ.Sockets;

namespace TrumpetCat
{
    public static class Trumpet
    {
        static NetMQContext context = NetMQContext.Create ();

        public static readonly string Address = "127.0.0.1";
        public static readonly int RequestPort = 5555;
        public static readonly int PublisherPort = 5556;


        public static void Blow (string song, string notes)
        {
            new Thread (() => {
                using (NetMQSocket request_socket = context.CreateRequestSocket ()) {
                    request_socket.Connect (string.Format ("tcp://{0}:{1}", Address, RequestPort));
                    request_socket.SendMore ("blow").SendMore (song).Send (notes);

                    string response = request_socket.ReceiveString ();
                    Console.WriteLine ("[request_socket] Response: {0}", response);

                    request_socket.Close ();
                    request_socket.Dispose ();
                }
            }).Start ();
        }


        static Thread thread;
        static SubscriberSocket subscriber_socket;

        public static void Listen (string song, string address = null)
        {
            if (address == null)
                address = Address;

            if (subscriber_socket == null) {
                subscriber_socket = context.CreateSubscriberSocket ();
                subscriber_socket.Connect (string.Format ("tcp://{0}:{1}", address, PublisherPort));
            }

            subscriber_socket.Subscribe (song);

            if (thread == null) {
                thread = new Thread (() => OnReceived (subscriber_socket));
                thread.Start ();
            }
        }


        public static event BlownEventHandler Blowed = delegate { };
        public delegate void BlownEventHandler (string song, string notes);

        static void OnReceived (NetMQSocket subscriber_socket)
        {
            while (true) {
                string song = subscriber_socket.ReceiveString ();
                string notes = subscriber_socket.ReceiveString ();

                Blowed (song, notes);
                Console.WriteLine ("[subcriber_socket] Received: song: {0}, notes: {1}", song, notes);
            }
        }
    }


    public class TrumpetCatException : Exception
    {
    }
}
