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
using System.Threading.Tasks;

namespace TrumpetCat
{
    class Tests
    {
        static void Main (string[] args)
        {
            Task server_task = Task.Factory.StartNew (() => Server  ());
            Task client_listener_task = Task.Factory.StartNew (() => ClientListener ());
            Task client_sender_task = Task.Factory.StartNew (() => ClientSender ());

            Task.WaitAll (server_task, client_listener_task, client_sender_task);
        }


        static void Server ()
        {
            var server = new TrumpetCatServer ();
            server.Start ();
        }

    
        static void ClientListener ()
        {
            TrumpetCat.Blowed += delegate (string song, string notes) {
                Console.WriteLine ("TrumpetCat blowed! ({0}, {1})", song, notes);
            };

            TrumpetCat.Listen ("kittens", "127.0.0.1");
            TrumpetCat.Listen ("puppies", "127.0.0.1");

            while (true)
                Thread.Sleep (1000);
        }


        static void ClientSender ()
        {
            while (true) {
                Thread.Sleep (2000);

                TrumpetCat.Blow ("kittens", "meow");
                TrumpetCat.Blow ("puppies", "woof");
                TrumpetCat.Blow ("puppies", "another woof");
            }
        }
    }
}
