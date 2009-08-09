﻿#region License
// Copyright 2009 Buu Nguyen (http://www.buunguyen.net/blog)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at http://fasterflect.codeplex.com/
#endregion

using System;
using System.Linq;
using System.Reflection;
using Fasterflect;

namespace FasterflectSample
{
    class Program
    {
        static void Main()
        {
            // Load a type reflectively, just to look like real-life scenario
            Type type = Assembly.GetExecutingAssembly().GetType("FasterflectSample.Person");
            ExecuteNormalApi(type);
            ExecuteCacheApi(type);
        }

        private static void ExecuteNormalApi(Type type)
        {
            // Person.InstanceCount should be 0 since no instance is created yet
            AssertTrue(type.GetField<int>("InstanceCount") == 0);
            
            // Invokes the no-arg constructor
            object obj = type.Construct();

            // Double-check if the constructor is invoked successfully or not
            AssertTrue(null != obj);

            // Now, Person.InstanceCount should be 1
            AssertTrue(1 == type.GetField<int>("InstanceCount"));
            
            // What if we don't know the type of InstanceCount?  
            // Just specify object as the type parameter
            Console.WriteLine(type.GetField<object>("InstanceCount"));

            // We can bypass the constructor to change the value of Person.InstanceCount
            type.SetField("InstanceCount", 2);
            AssertTrue(2 == type.GetField<int>("InstanceCount"));

            // Let's invoke Person.IncreaseCounter() static method to increase the counter
            // In fact, let's chain the calls to increase 2 times
            type.Invoke("IncreaseInstanceCount")
                .Invoke("IncreaseInstanceCount");
            AssertTrue(4 == type.GetField<int>("InstanceCount"));

            // Now, let's retrieve Person.InstanceCount via the static method GetInstanceCount
            AssertTrue(4 == type.Invoke<int>("GetInstanceCount"));

            // If we're not interested in the return (e.g. only in the side effect), 
            // we don't have to specify the type parameter (and can chain the result).  
            AssertTrue(4 == type.Invoke("GetInstanceCount")
                                .Invoke("GetInstanceCount")
                                .Invoke<int>("GetInstanceCount"));

            // Now, invoke the 2-arg constructor
            obj = type.Construct(new[] {typeof (int), typeof (string)}, new object[] {1, "Doe"});

            // The id field should be 1, so is Id property
            AssertTrue(1 == obj.GetField<int>("id"));
            AssertTrue(1 == obj.GetProperty<int>("Id"));

            // Now, modify the id
            obj.SetField("id", 2);
            AssertTrue(2 == obj.GetField<int>("id"));
            AssertTrue(2 == obj.GetProperty<int>("Id"));

            // Let's use the indexer to retrieve the character at index 1st
            AssertTrue('o' == obj.GetIndexer<char>(new[] {typeof (int)}, new object[] {1}));

            // We can chain calls
            obj.SetField("id", 3).SetProperty("Name", "Buu");
            AssertTrue(3 == obj.GetProperty<int>("Id"));
            AssertTrue("Buu" == obj.GetProperty<string>("Name"));
             
            // How about modifying both properties at the same time using an anonymous sample
            obj.SetProperties(new {Id = 4, Name = "Nguyen"});
            AssertTrue(4 == obj.GetProperty<int>("Id"));
            AssertTrue("Nguyen" == obj.GetProperty<string>("Name"));

            // Let's have the folk walk 6 miles (and try chaining again)
            obj.Invoke("Walk", new[] { typeof(int) }, new object[] { 1 })
               .Invoke("Walk", new[] { typeof(int) }, new object[] { 2 })
               .Invoke("Walk", new[] { typeof(int) }, new object[] { 3 });

            // Double-check the current value of the milesTravelled field
            AssertTrue(6 == obj.GetField<int>("milesTraveled"));
        }

        private static void ExecuteCacheApi(Type type)
        {
            var range = Enumerable.Range(0, 10).ToList();

            // Let's cache the getter for InstanceCount
            StaticAttributeGetter count = type.DelegateForGetStaticField("InstanceCount");

            // Now cache the 2-arg constructor of Person and playaround with the delegate returned
            int currentInstanceCount = (int)count();
            ConstructorInvoker ctor = type.DelegateForConstruct(new[] { typeof(int), typeof(string) });
            range.ForEach(i =>
            {
                var obj = ctor(i, "_" + i);
                AssertTrue(++currentInstanceCount == (int)count());
                AssertTrue(i == obj.GetField<int>("id"));
                AssertTrue("_" + i == obj.GetProperty<string>("Name"));
            });

            // Whatever thing we can do with the normal API, we can do with the cache API.
            // For example:
            AttributeSetter nameSetter = type.DelegateForSetProperty("Name");
            AttributeGetter nameGetter = type.DelegateForGetProperty("Name");
            object person = ctor(1, "Buu");
            AssertTrue("Buu" == nameGetter(person));
            nameSetter(person, "Doe");
            AssertTrue("Doe" == nameGetter(person));

            // Another example
            person = type.Construct();
            MethodInvoker walk = type.DelegateForInvoke("Walk", new[] { typeof(int) });
            range.ForEach(i => walk(person, i));
            AssertTrue(range.Sum() == person.GetField<int>("milesTraveled"));
        }

        public static void AssertTrue(bool expression)
        {
            if (!expression)
                throw new Exception("Not true");
            Console.WriteLine("Ok!");
        }
    }
}
