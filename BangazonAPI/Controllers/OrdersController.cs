﻿using BangazonAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace BangazonAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IConfiguration _config;

        public OrdersController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(string include)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    string command = "";
                    string ordersColumns = "SELECT o.Id, o.PaymentTypeId, o.CustomerId";
                    string ordersTable = "FROM [Order] o";

                    if (include == "products")
                    {
                        string productColumns = @", 
                        p.Id AS 'Product Id', 
                        p.Title AS 'Product Title'";
                        string productTable = @"
                        JOIN OrderProduct op ON o.Id = op.OrderId 
                        JOIN Product p ON op.ProductId = p.Id;";
                        command = $@"{ordersColumns} 
                                    {productColumns} 
                                    {ordersTable} 
                                    {productTable}";

                    }
                    if (include == "customers")
                    {
                        string customerColumns = @",
                        c.FirstName AS 'Customer First'
                        c.LastName AS 'Customer Last'";
                        string customerTable = @"
                        JOIN PaymentType pt ON o.Id = pt.PaymentTypeId 
                        JOIN Customer c ON pt.CustomerId = c.Id;";
                        command = $@"{ordersColumns} 
                                    {customerColumns} 
                                    {ordersTable} 
                                    {customerTable}";

                    }
                    else
                    {
                        command = $"{ordersColumns} {ordersTable}";
                    }

                    cmd.CommandText = command;
                    SqlDataReader reader = cmd.ExecuteReader();
                    List<Order> Orders = new List<Order>();

                    while (reader.Read())
                    {

                        Order Order = new Order
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            PaymentTypeId = reader.GetInt32(reader.GetOrdinal("PaymentTypeId")),
                            CustomerId = reader.GetInt32(reader.GetOrdinal("CustomerId"))
                        };

                        if (include == "student")
                        {
                            currentStudent = new Student
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Student Id")),
                                FirstName = reader.GetString(reader.GetOrdinal("Student First Name")),
                                LastName = reader.GetString(reader.GetOrdinal("Student Last Name")),
                                SlackHandle = reader.GetString(reader.GetOrdinal("Slack Handle"))
                            };

                            if (Exercises.Any(e => e.id == Order.id))
                            {
                                Order thisExercise = Order.Where(e => e.id == Order.id).FirstOrDefault();
                                thisExercise.assignedStudents.Add(currentStudent);
                            }
                            else
                            {
                                Exercise.assignedStudents.Add(currentStudent);
                                Exercises.Add(Exercise);

                            }

                        }
                        else
                        {
                            Exercises.Add(Order);

                        }

                    }
                    reader.Close();


                    return Ok(Exercises);
                }
            }
        }

        [HttpGet("{id}", Name = "GetExercise")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT
                            Id, name, language
                        FROM Exercise
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Exercise Exercise = null;

                    if (reader.Read())
                    {
                        Exercise = new Exercise
                        {
                            id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            Language = reader.GetString(reader.GetOrdinal("lastName"))

                        };
                    }
                    reader.Close();

                    return Ok(Exercise);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Exercise Exercise)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Exercise (name, language)
                                        OUTPUT INSERTED.Id
                                        VALUES (@name, @lang)";
                    cmd.Parameters.Add(new SqlParameter("@name", Exercise.Name));
                    cmd.Parameters.Add(new SqlParameter("@lang", Exercise.Language));
                    int newId = (int)cmd.ExecuteScalar();
                    Exercise.id = newId;
                    return CreatedAtRoute("GetExercise", new { id = newId }, Exercise);
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Exercise Exercise)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Exercise
                                            SET name=@n, 
                                            language=@lang, 
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@n", Exercise.Name));
                        cmd.Parameters.Add(new SqlParameter("@lang", Exercise.Language));
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!ExerciseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Exercise WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!ExerciseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool ExerciseExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT
                            name, language
                        FROM Exercise
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
    }
}