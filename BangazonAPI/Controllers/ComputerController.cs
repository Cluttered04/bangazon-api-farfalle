﻿//**********************************************************************************************//
// This Controller gives the client access to getAll, getSingle, Post, Put, and Delete 
// (soft delete updates the decomission date to a non-null value)
// on the Computer Resource.  It does not support query functionality at this point.
// Created by Sydney Wait
//*********************************************************************************************//



using BangazonAPI.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;


namespace BangazonAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ComputerController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ComputerController(IConfiguration config)
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


        //GET:Code for getting a list of Computers which are ACTIVE in the system
        [HttpGet]
        public async Task<IActionResult> GetAllComputers()
        {

            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    string commandText = $"SELECT Id, PurchaseDate, Make, Manufacturer FROM Computer WHERE DecomissionDate IS NULL";

                    cmd.CommandText = commandText;

                    SqlDataReader reader = cmd.ExecuteReader();
                    List<Computer> computers = new List<Computer>();
                    Computer computer = null;


                    while (reader.Read())
                    {
                        computer = new Computer
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            PurchaseDate = reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                            Make = reader.GetString(reader.GetOrdinal("Make")),
                            Manufacturer = reader.GetString(reader.GetOrdinal("Manufacturer"))
                        };


                        computers.Add(computer);
                    }


                    reader.Close();

                    return Ok(computers);
                }
            }
        }



        //GET: Code for getting a single computer (active or not)
        [HttpGet("{id}", Name = "Computer")]
        public async Task<IActionResult> GetSingleComputer([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"SELECT Id, PurchaseDate, Make, Manufacturer, DecomissionDate FROM Computer WHERE id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Computer computerToDisplay = null;

                    while (reader.Read())
                    {

                        {
                            computerToDisplay = new Computer
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                PurchaseDate = reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                                Make = reader.GetString(reader.GetOrdinal("Make")),
                                Manufacturer = reader.GetString(reader.GetOrdinal("Manufacturer"))

                            };

                            //Check to see if the decomission date is null.If not, add it to the object.  If it is, set the Decomission Date to null on the object.

                            if (!reader.IsDBNull(reader.GetOrdinal("DecomissionDate")))
                            {
                                computerToDisplay.DecomissionDate = reader.GetDateTime(reader.GetOrdinal("DecomissionDate"));
                            }
                            else
                            { 
                                computerToDisplay.DecomissionDate = DateTime.MinValue;
                            }
                        };
                    };


                    reader.Close();

                    return Ok(computerToDisplay);
                }
            }
        }

        //  POST: Code for creating a computer
        [HttpPost]
        public async Task<IActionResult> Computer([FromBody] Computer computer)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {

                    cmd.CommandText = $@"INSERT INTO Computer (PurchaseDate, DecomissionDate, Make, Manufacturer)
                                                    OUTPUT INSERTED.Id
                                                    VALUES (@PurchaseDate, Null, @Make,          @Manufacturer)";
                    cmd.Parameters.Add(new SqlParameter("@PurchaseDate", computer.PurchaseDate));
                    cmd.Parameters.Add(new SqlParameter("@Make", computer.Make));
                    cmd.Parameters.Add(new SqlParameter("@Manufacturer", computer.Manufacturer));

                    int newId = (int)cmd.ExecuteScalar();
                    computer.Id = newId;
                    computer.DecomissionDate = DateTime.MinValue;

                    return CreatedAtRoute("Computer", new { id = newId }, computer);
                }
            }
        }

        // PUT: Code for editing a computer
        [HttpPut("{id}")]
        public async Task<IActionResult> PutComputer([FromRoute] int id, [FromBody] Computer computer)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Computer
                                                        SET PurchaseDate = @PurchaseDate,
                                                        DecomissionDate = Null,
                                                        Make=@Make,
                                                        Manufacturer = @Manufacturer
                                                        WHERE id = @id";

                        cmd.Parameters.Add(new SqlParameter("@PurchaseDate", computer.PurchaseDate));
                        cmd.Parameters.Add(new SqlParameter("@Make", computer.Make));
                        cmd.Parameters.Add(new SqlParameter("@Manufacturer", computer.Manufacturer));
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
                if (!ComputerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // DELETE: Code for deleting a payment type--soft delete actually changes Deocmmission date to a non-null value
        [HttpDelete("{id}")]
        public async Task<IActionResult> Computer([FromRoute] int id, bool HardDelete)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {

                        if (HardDelete == true)
                        {
                            cmd.CommandText = @"DELETE Computer
                                              WHERE id = @id";
                        }
                        else
                        {
                            cmd.CommandText = @"UPDATE Computer
                                            SET DecomissionDate = @currentDate
                                            WHERE id = @id";
                        }

                        //Set the current date to today
                        DateTime currentDate = DateTime.Now;                            

                        cmd.Parameters.Add(new SqlParameter("@id", id));
                        cmd.Parameters.Add(new SqlParameter("@currentDate", currentDate));

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
                if (!ComputerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }


        private bool ComputerExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, Make
                                    FROM Computer
                                    WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }


    }
}




