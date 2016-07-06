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
using System.Collections;
using System.Threading.Tasks;

using NetMQ;

namespace TrumpetCat.Server
{
    public class Server
    {
        public readonly string Address = "127.0.0.1";

        public readonly int ResponsePort = 5555;
        public readonly int PublisherPort = 5556;

        Hashtable cache = new Hashtable ();
        NetMQContext context = NetMQContext.Create ();


        static void Main (string [] args)
        {
            var server = new Server ();

            Task server_task = Task.Factory.StartNew (() => server.Start ());
            Task.WaitAll (server_task);
        }


        public void Start ()
        {
            using (NetMQSocket response_socket = context.CreateResponseSocket ())
            using (NetMQSocket publisher_socket = context.CreateXPublisherSocket ()) {

                string response_address = string.Format ("tcp://{0}:{1}", Address, ResponsePort);
                string publisher_address = string.Format ("tcp://{0}:{1}", Address, PublisherPort);

                response_socket.Bind (response_address);
                publisher_socket.Bind (publisher_address);

                Console.WriteLine ("[response_socket] Bound on {0}", response_address);
                Console.WriteLine ("[publisher_socket] Bound on {0}", publisher_address);

                using (Poller poller = new Poller (response_socket, publisher_socket)) {
                    response_socket.ReceiveReady += delegate (object sender, NetMQSocketEventArgs args) {
                        string message = response_socket.ReceiveString ();

                        if (message.StartsWith ("blow")) {
                            string song = "";
                            string notes = "";

                            try {
                                song = response_socket.ReceiveString ();
                                notes = response_socket.ReceiveString ();

                                if (song.Length > 64)
                                    song = song.Substring (0, 64);

                                if (notes.Length > 64)
                                    notes = notes.Substring (0, 64);

                                cache [song] = notes;

                                Console.WriteLine ("[response_socket] Received: song: {0}, notes: {1}", song, notes);

                            } catch (Exception e) {
                                Console.WriteLine ("[response_socket] Invalid request: {0}", e.Message);
                            }

                            response_socket.Send ("OK");
                            Console.WriteLine ("[response_socket] Sent: OK");

                            publisher_socket.SendMore (song).Send (notes);

                            Console.WriteLine ("[publisher_socket] Sent: song: {0}, notes: {1}", song, notes);
                            return;
                        }

                        if (message.Equals ("ping")) {
                            Console.WriteLine ("[response_socket] Received: {0}", message);

                            int timestamp = (int)DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1)).TotalSeconds;
                            response_socket.Send (timestamp.ToString ());

                            Console.WriteLine ("[response_socket] Sent: {0}", timestamp);
                            return;
                        }

                        Console.WriteLine ("[response_socket] Invalid request: {0}", message);
                        args.Socket.Send ("Meow?");
                    };

                    // Send cached notes to new subscribers
                    publisher_socket.ReceiveReady += delegate (object sender, NetMQSocketEventArgs args) {
                        NetMQMessage message = publisher_socket.ReceiveMessage ();

                        // Subscribe == 1, Unsubscibe == 0
                        if (message.First.Buffer [0] != 1)
                            return;

                        string song = message.First.ConvertToString ().Substring (1);
                        string cached_notes = (string)cache [song];

                        if (cached_notes != null)
                            publisher_socket.SendMore (song).Send (cached_notes);
                    };

                    poller.Start ();
                }
            }
        }
    }
}
