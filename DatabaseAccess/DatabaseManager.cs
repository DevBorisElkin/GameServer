﻿using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseAccess
{
    /// <summary>
    /// Is responsible for getting/setting data from/to DB. logins are
    /// not case sensitive, passwords are case sensitive
    /// </summary>
    public static class DatabaseManager
    {
        static MySqlConnection mySqlConnection;

        #region Connect/Disconnect
        public static void Connect()
        {
            string connectionString = File.ReadAllText(@"C:\MyProjectPasswords\MultiplayerGame_1\AccessToDataBase.txt");
            mySqlConnection = new MySqlConnection(connectionString);
            mySqlConnection.Open();

            if (mySqlConnection.State == System.Data.ConnectionState.Open)
            {
                Console.WriteLine("[SERVER_MESSAGE]: Successfully connected to database");

                MySqlCommand commandUse_database = new MySqlCommand($"USE MainData;", mySqlConnection);
                MySqlDataReader readerUse_database = commandUse_database.ExecuteReader();
                readerUse_database.Close();
            }
            else Console.WriteLine($"[SERVER_MESSAGE]: Failed to connect to database, Connection State: {mySqlConnection.State}");
        }

        public static void Disconnect()
        {
            if (mySqlConnection.State == System.Data.ConnectionState.Open)
            {
                mySqlConnection.Close();
                Console.WriteLine("Database connection closed");
            }
        }
        #endregion

        /// <summary>
        /// Checks whether user with speific 'login' and 'password' exists. If such user exists it
        /// returns UserData from Database, if not, returns null; 
        /// </summary>
        public static async Task<UserData> TryToAuthenticateAsync(UserData info)
        {
            try
            {
                string login = info.login;
                string password = info.password;


                if (mySqlConnection.State == System.Data.ConnectionState.Open)
                {
                    MySqlCommand command = new MySqlCommand($"SELECT * FROM MainTable WHERE login = '{login}' AND pass = binary '{password}'", mySqlConnection);
                    //MySqlDataReader reader = command.ExecuteReader();
                    var reader = await command.ExecuteReaderAsync();
                    

                    if (await reader.ReadAsync())
                    {
                        int id = Int32.Parse(reader["id"].ToString());
                        string nickname = reader["nickname"].ToString();
                        UserData userData = new UserData(id, login, password, nickname);
                        reader.Close();
                        return userData;
                    }
                    else
                    {
                        // didn't find any record of 'login' + 'password'
                        await reader.CloseAsync();
                        return new UserData(RequestResult.Fail_WrongPairLoginPassword);
                    }
                }
                else
                {
                    Console.WriteLine($"Can't interact with Database because connection state is {mySqlConnection.State}");
                    return new UserData(RequestResult.Fail_NoConnectionToDB);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.ToString()}");
            }
            return new UserData(RequestResult.Fail);
        }

        public static bool TryToDeleteUser(int id)
        {
            int rowsAffected = 0;
            try
            {
                MySqlCommand command = new MySqlCommand($"DELETE from MainTable where id = {id}", mySqlConnection);
                rowsAffected = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.ToString()}");
            }

            return rowsAffected == 1;
        }

        /// <summary>
        /// Deprecated. Use TryToUpdateUserData() instead
        /// </summary>
        public static bool TryToChangeData(UserData _old, UserData _new)
        {
            int rowsAffected = 0;
            try
            {
                MySqlCommand command = new MySqlCommand($"UPDATE MainTable SET login = '{_new.login}', pass = '{_new.password}', " +
                    $"nickname = '{_new.nickname}' where id = '{_old.id}'", mySqlConnection);
                rowsAffected = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.ToString()}");
            }

            return rowsAffected == 1;
        }

        /// <summary>
        /// Updates UserData in database
        /// </summary>
        public static bool TryToUpdateUserData(UserData _new)
        {
            int rowsAffected = 0;
            try
            {
                MySqlCommand command = new MySqlCommand($"UPDATE MainTable SET login = '{_new.login}', pass = '{_new.password}', " +
                    $"nickname = '{_new.nickname}' where id = '{_new.id}'", mySqlConnection);
                rowsAffected = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.ToString()}");
            }

            return rowsAffected == 1;
        }

        public static UserData TryToGetUserDataByLogin(string login)
        {
            try
            {
                MySqlCommand findUser = new MySqlCommand($"SELECT * FROM MainTable WHERE login = '{login}'", mySqlConnection);
                MySqlDataReader findUserReader = findUser.ExecuteReader();

                if (findUserReader.Read())
                {
                    int resultId = Int32.Parse(Convert.ToString(findUserReader["id"]));
                    string resultLogin = Convert.ToString(findUserReader["login"]);
                    string resultPassword = Convert.ToString(findUserReader["pass"]);
                    string resultNickname = Convert.ToString(findUserReader["nickname"]);

                    findUserReader.Close();
                    return new UserData(resultId, resultLogin, resultPassword, resultNickname);
                }
                else
                {
                    findUserReader.Close();
                    return new UserData(RequestResult.Fail_NoUserWithGivenLogin);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.ToString()}");
            }
            return new UserData(RequestResult.Fail);
        }

        public static UserData TryToRegisterAsync(object _userData)
        {
            try
            {
                UserData info = (UserData)_userData;

                if (mySqlConnection.State == System.Data.ConnectionState.Open)
                {   
                    // check if user with such login already exists
                    MySqlCommand command = new MySqlCommand($"SELECT * FROM MainTable WHERE login = '{info.login}'", mySqlConnection);
                    MySqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        // can't register, login is already taken
                        reader.Close();
                        return new UserData(RequestResult.Fail_LoginAlreadyTaken);
                    }
                    else
                    {
                        // didn't find any record of 'login', keep going
                        reader.Close();

                        // here need to try to add new user
                        MySqlCommand sqlCommand = new MySqlCommand($"INSERT INTO MainTable (login, pass, nickname)" +
                            $" VALUES ('{info.login}', '{info.password}', '{info.nickname}')", mySqlConnection);
                        int rowsAffected = sqlCommand.ExecuteNonQuery();

                        if (rowsAffected == 1)
                        {
                            // check that we actually added user and retrieve his full data
                            UserData foundUser = TryToGetUserDataByLogin(info.login);
                            bool successfullyFoundUser = foundUser.requestResult.Equals(RequestResult.Success);
                            if (successfullyFoundUser)
                            {
                                return foundUser;
                            }
                            else return new UserData(RequestResult.Fail);

                        }
                        else return new UserData(RequestResult.Fail);
                    }
                }
                else
                {
                    Console.WriteLine($"Can't interact with Database because connection state is {mySqlConnection.State}");
                    return new UserData(RequestResult.Fail_NoConnectionToDB);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.ToString()}");
            }
            return new UserData(RequestResult.Fail);
        }
    }
}
