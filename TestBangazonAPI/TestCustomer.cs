﻿using Newtonsoft.Json;
using BangazonAPI.Models;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Linq;

namespace TestBangazonAPI
{

    public class TestCustomer
    {

        // Since we need to clean up after ourselves, we'll create and delete a student when we test POST and PUT
        // Otherwise, eveyr time we ran our test suite it would create a new David entry and we'd end up with a tooon of Davids

        // Create a new student in the db and make sure we get a 200 OK status code back
        public async Task<Customer> createDavid(HttpClient client)
        {
            Customer david = new Customer
            {
                FirstName = "David",
                LastName = "Bird",
            };
            string davidAsJSON = JsonConvert.SerializeObject(david);


            HttpResponseMessage response = await client.PostAsync(
                "api/customer",
                new StringContent(davidAsJSON, Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            Customer newDavid = JsonConvert.DeserializeObject<Customer>(responseBody);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            return newDavid;

        }

        // Delete a student in the database and make sure we get a no content status code back
        public async Task deleteDavid(Customer david, HttpClient client)
        {
            HttpResponseMessage deleteResponse = await client.DeleteAsync($"api/customer/{david.Id}");
            deleteResponse.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        }


        [Fact]
        public async Task Test_Get_All_Customer()
        {
            // Use the http client
            using (HttpClient client = new APIClientProvider().Client)
            {

                // Call the route to get all our students; wait for a response object
                HttpResponseMessage response = await client.GetAsync("api/customer");

                // Make sure that a response comes back at all
                response.EnsureSuccessStatusCode();

                // Read the response body as JSON
                string responseBody = await response.Content.ReadAsStringAsync();

                // Convert the JSON to a list of student instances
                List<Customer> customerList = JsonConvert.DeserializeObject<List<Customer>>(responseBody);

                // Did we get back a 200 OK status code?
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Are there any students in the list?
                Assert.True(customerList.Count > 0);
            }
        }

        [Fact]
        public async Task Test_Get_Single_Customer()
        {

            using (HttpClient client = new APIClientProvider().Client)
            {

                // Create a new student
                Customer newDavid = await createDavid(client);

                // Try to get that student from the database
                HttpResponseMessage response = await client.GetAsync($"api/customer/{newDavid.Id}");

                response.EnsureSuccessStatusCode();

                // Turn the response into JSON
                string responseBody = await response.Content.ReadAsStringAsync();

                // Turn the JSON into C#
                Customer customer = JsonConvert.DeserializeObject<Customer>(responseBody);

                // Did we get back what we expected to get back? 
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("David", newDavid.FirstName);
                Assert.Equal("Bird", newDavid.LastName);

                // Clean up after ourselves- delete david!
                deleteDavid(newDavid, client);
            }
        }

        [Fact]
        public async Task Test_Include_products()
        {

            using (HttpClient client = new APIClientProvider().Client)
            {

                // Create a new student
                Customer newDavid = await createDavid(client);

                // Try to get that student from the database
                HttpResponseMessage response = await client.GetAsync($"api/customer/?include=products");

                response.EnsureSuccessStatusCode();

                // Turn the response into JSON
                string responseBody = await response.Content.ReadAsStringAsync();

                // Turn the JSON into C#
                List<Customer> customerList = JsonConvert.DeserializeObject<List<Customer>>(responseBody);
                // Did we get back what we expected to get back? 
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                

                // Clean up after ourselves- delete david!
                deleteDavid(newDavid, client);
            }
        }

        [Fact]
        public async Task Test_Get_Inactive_Customers()
        {

            using (HttpClient client = new APIClientProvider().Client)
            {

                // Create a new customer
                Customer newDavid = await createDavid(client);

                // Try to get that customer from the database
                HttpResponseMessage response = await client.GetAsync($"api/customer/?active=false");

                // Make sure that a response comes back at all
                response.EnsureSuccessStatusCode();

                // Read the response body as JSON
                string responseBody = await response.Content.ReadAsStringAsync();

                // Convert the JSON to a list of customer instances
                List<Customer> customerList = JsonConvert.DeserializeObject<List<Customer>>(responseBody);

                // Did we get back a 200 OK status code?
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // Are there any customers in the list?
                Assert.True(customerList.Count > 0);


                // Clean up after ourselves- delete david!
                deleteDavid(newDavid, client);
            }
        }

        [Fact]
        public async Task Test_Include_Query_payment()
        {

            using (HttpClient client = new APIClientProvider().Client)
            {

                // Create a new student
                Customer newDavid = await createDavid(client);

                // Try to get that student from the database
                HttpResponseMessage response = await client.GetAsync($"api/customer/?include=paymentTypes");

                response.EnsureSuccessStatusCode();

                // Turn the response into JSON
                string responseBody = await response.Content.ReadAsStringAsync();

                // Turn the JSON into C#
                List<Customer> customerList = JsonConvert.DeserializeObject<List<Customer>>(responseBody);
                // Did we get back what we expected to get back? 
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                

                // Clean up after ourselves- delete david!
                deleteDavid(newDavid, client);
            }
        }


        [Fact]
        public async Task Test_Query_String_q()
        {

            using (HttpClient client = new APIClientProvider().Client)
            {

                // Create a new student
                Customer newDavid = await createDavid(client);

                // Try to get that student from the database
                HttpResponseMessage response = await client.GetAsync($"api/customer/{newDavid.Id}/?q=david");

                response.EnsureSuccessStatusCode();

                // Turn the response into JSON
                string responseBody = await response.Content.ReadAsStringAsync();

                // Turn the JSON into C#
                Customer customer = JsonConvert.DeserializeObject<Customer>(responseBody);

                // Did we get back what we expected to get back? 
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("David", newDavid.FirstName);
                Assert.Equal("Bird", newDavid.LastName);

                // Clean up after ourselves- delete david!
                deleteDavid(newDavid, client);
            }
        }


        [Fact]
        public async Task Test_Get_NonExitant_Customer_Fails()
        {

            using (var client = new APIClientProvider().Client)
            {
                // Try to get a student with an enormously huge Id
                HttpResponseMessage response = await client.GetAsync("api/customer/999999999");

                // It should bring back a 204 no content error
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
        }


        [Fact]
        public async Task Test_Create_And_Delete_Student()
        {
            using (var client = new APIClientProvider().Client)
            {

                // Create a new David
                Customer newDavid = await createDavid(client);

                // Make sure his info checks out
                Assert.Equal("David", newDavid.FirstName);
                Assert.Equal("Bird", newDavid.LastName);

                // Clean up after ourselves - delete David!
                deleteDavid(newDavid, client);
            }
        }

        [Fact]
        public async Task Test_Delete_NonExistent_Customer_Fails()
        {
            using (var client = new APIClientProvider().Client)
            {
                // Try to delete an Id that shouldn't exist in the DB
                HttpResponseMessage deleteResponse = await client.DeleteAsync("/api/customer/600000");
                Assert.False(deleteResponse.IsSuccessStatusCode);
                Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
            }
        }

        [Fact]
        public async Task Test_Modify_Customer()
        {

            // We're going to change a student's name! This is their new name.
            string newFirstName = "Super cool dude";

            using (HttpClient client = new APIClientProvider().Client)
            {

                // Create a new student
                Customer newDavid = await createDavid(client);

                // Change their first name
                newDavid.FirstName = newFirstName;

                // Convert them to JSON
                string modifiedDavidAsJSON = JsonConvert.SerializeObject(newDavid);

                // Make a PUT request with the new info
                HttpResponseMessage response = await client.PutAsync(
                    $"api/customer/{newDavid.Id}",
                    new StringContent(modifiedDavidAsJSON, Encoding.UTF8, "application/json")
                );


                response.EnsureSuccessStatusCode();

                // Convert the response to JSON
                string responseBody = await response.Content.ReadAsStringAsync();

                // We should have gotten a no content status code
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

                /*
                    GET section
                 */
                // Try to GET the student we just edited
                HttpResponseMessage getDavid = await client.GetAsync($"api/customer/{newDavid.Id}");
                getDavid.EnsureSuccessStatusCode();

                string getDavidBody = await getDavid.Content.ReadAsStringAsync();
                Customer modifiedDavid = JsonConvert.DeserializeObject<Customer>(getDavidBody);

                Assert.Equal(HttpStatusCode.OK, getDavid.StatusCode);

                // Make sure his name was in fact updated
                Assert.Equal(newFirstName, modifiedDavid.FirstName);

                // Clean up after ourselves- delete him
                deleteDavid(modifiedDavid, client);
            }
        }
    }
}
