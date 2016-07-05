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

namespace TrumpetCat
{
    class TrumpetCat
    {
        static void Main (string [] args)
        {
            var server = new TrumpetCatServer ();

            Task server_task = Task.Factory.StartNew (() => server.Start ());
            Task.WaitAll (server_task);
        }
    }


    public class TrumpetCatServer
    {
        string Address = "127.0.0.1";

        int ResponsePort = 5555;
        int PublisherPort = 5556;

        NetMQContext context = NetMQContext.Create ();
        Hashtable cache = new Hashtable ();


        // TODO: Last Value Cache
        public void Start ()
        {
            using (NetMQSocket response_socket = context.CreateResponseSocket ())
            using (NetMQSocket publisher_socket = context.CreatePublisherSocket ()) {
                string response_address = string.Format ("tcp://{0}:{1}", Address, ResponsePort);
                string publisher_address = string.Format ("tcp://{0}:{1}", Address, PublisherPort);

                response_socket.Bind (response_address);
                publisher_socket.Bind (publisher_address);

                Console.WriteLine ("[response_socket] Bound on {0}", response_address);
                Console.WriteLine ("[publisher_socket] Bound on {0}", publisher_address);

                while (true) {
                    string message = response_socket.ReceiveString ();

                    if (message.Equals ("ping")) {
                        Console.WriteLine ("[response_socket] Received: {0}", message);

                        int timestamp = (int) DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1)).TotalSeconds;
                        response_socket.Send (timestamp.ToString ());

                        Console.WriteLine ("[response_socket] Sent: {0}", timestamp);

                    } else if (message.StartsWith ("blow")) {
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

                        if (!string.IsNullOrEmpty (song) && !string.IsNullOrEmpty (notes)) {
                            response_socket.Send ("OK");
                            Console.WriteLine ("[response_socket] Sent: OK");

                            publisher_socket.SendMore (song).Send (notes);
                            Console.WriteLine ("[publisher_socket] Sent: song: {0}, notes: {1}", song, notes);
                        }

                    } else {
                        Console.WriteLine ("[response_socket] Invalid request: {0}", message);
                        response_socket.Send ("Meow?");
                    }
                }
            }
        }
    }
}
