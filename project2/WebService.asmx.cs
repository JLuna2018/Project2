using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

// we need these to talk to mysql
using MySql.Data;
using MySql.Data.MySqlClient;
// and we need this to manipulate data from a db
using System.Data;


namespace project2
{
    /// <summary>
    /// Summary description for WebService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService : System.Web.Services.WebService
    {

        [WebMethod]
        public int NumberOfAccounts()
        {
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["teamnala"].ConnectionString;
            string sqlSelect = "SELECT * from employee";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            DataTable sqlDt = new DataTable();
            sqlDa.Fill(sqlDt);

            return sqlDt.Rows.Count;

        }

        //EXAMPLE OF A SIMPLE SELECT QUERY (PARAMETERS PASSED IN FROM CLIENT)
        [WebMethod(EnableSession = true)] //NOTICE: gotta enable session on each individual method
        public bool LogOn(string username)
        {
            //we return this flag to tell them if they logged in or not
            bool success = false;

            //our connection string comes from our web.config file like we talked about earlier
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["teamnala"].ConnectionString;
            //here's our query.  A basic select with nothing fancy.  Note the parameters that begin with @
            //NOTICE: we added admin to what we pull, so that we can store it along with the id in the session
            string sqlSelect = "SELECT username FROM employee WHERE username=@usernameValue";

            //set up our connection object to be ready to use our connection string
            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            //set up our command object to use our connection, and our query
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            //tell our command to replace the @parameters with real values
            //we decode them because they came to us via the web so they were encoded
            //for transmission (funky characters escaped, mostly)
            sqlCommand.Parameters.AddWithValue("@usernameValue", HttpUtility.UrlDecode(username));

            //a data adapter acts like a bridge between our command object and 
            //the data we are trying to get back and put in a table object
            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            //here's the table we want to fill with the results from our query
            DataTable sqlDt = new DataTable();
            //here we go filling it!
            sqlDa.Fill(sqlDt);
            //check to see if any rows were returned.  If they were, it means it's 
            //a legit account
            if (sqlDt.Rows.Count > 0)
            {
                //if we found an account, store the id and admin status in the session
                //so we can check those values later on other method calls to see if they 
                //are 1) logged in at all, and 2) and admin or not
                Session["username"] = sqlDt.Rows[0]["username"];
                success = true;
            }
            //return the result!
            return success;
        }

        //EXAMPLE OF A SELECT, AND RETURNING "COMPLEX" DATA TYPES
        [WebMethod(EnableSession = true)]
        public mediaPost[] GetPosts()
        {

            if (Session["username"] != null)
            {
                DataTable sqlDt = new DataTable("posts");

                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["teamnala"].ConnectionString;
                string sqlSelect = "select postNumber, postText from mediaposts order by postNumber";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                //gonna use this to fill a data table
                MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
                //filling the data table
                sqlDa.Fill(sqlDt);

                //loop through each row in the dataset, creating instances
                //of our container class mediaPost.  Fill each mediaPost with
                //data from the rows, then dump them in a list.
                List<mediaPost> posts = new List<mediaPost>();
                for (int i = 0; i < sqlDt.Rows.Count; i++)
                {
                        posts.Add(new mediaPost
                        {
                            postNumber = Convert.ToInt32(sqlDt.Rows[i]["postNumber"]),
                            postText = sqlDt.Rows[i]["postText"].ToString(),
                            
                        });
                }
                //convert the list of posts to an array and return!
                return posts.ToArray();
            }
            else
            {
                //if they're not logged in, return an empty array
                return new mediaPost[0];
            }
        }


        [WebMethod(EnableSession = true)]
        public int IdeaSubmission(string postText)
        {
            //bool success = false;

            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["teamnala"].ConnectionString;
            //the only thing fancy about this query is SELECT LAST_INSERT_ID() at the end.  All that
            //does is tell mySql server to return the primary key of the last inserted row.
            string sqlSelect = "INSERT INTO mediaposts (postText) VALUES (@postValue);";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@postValue", HttpUtility.UrlDecode(postText));

            // Was trying to use this to see if query was actually affecting rows in the table
            sqlConnection.Open();
            int affectedRows = sqlCommand.ExecuteNonQuery();
            sqlConnection.Close();
            return affectedRows;

        }

        //EXAMPLE OF A SELECT, AND RETURNING "COMPLEX" DATA TYPES
        [WebMethod(EnableSession = true)]
        public questions[] GetQuestions()
        {
            //check out the return type.  It's an array of Account objects.  You can look at our custom Account class in this solution to see that it's 
            //just a container for public class-level variables.  It's a simple container that asp.net will have no trouble converting into json.  When we return
            //sets of information, it's a good idea to create a custom container class to represent instances (or rows) of that information, and then return an array of those objects.  
            //Keeps everything simple.

            //WE ONLY SHARE ACCOUNTS WITH LOGGED IN USERS!
            if (Session["username"] != null)
            {
                DataTable sqlDt = new DataTable("questions");

                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["teamnala"].ConnectionString;
                string sqlSelect =
                    "select questionText, questionCategory " +
                    "from surveyquestions " +
                    "where questionId = (select max(questionId) - 1 from surveyquestions) " + "or " +
                                        "questionId = (select max(questionId) from surveyquestions)";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                //gonna use this to fill a data table
                MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
                //filling the data table
                sqlDa.Fill(sqlDt);

                //loop through each row in the dataset, creating instances
                //of our container class mediaPost.  Fill each mediaPost with
                //data from the rows, then dump them in a list.
                List<questions> questionList = new List<questions>();
                for (int i = 0; i < sqlDt.Rows.Count; i++)
                {
                    questionList.Add(new questions
                    {
                        questionText = sqlDt.Rows[i]["questionText"].ToString(),
                        questionCategory = sqlDt.Rows[i]["questionCategory"].ToString(),

                    });
                }
                //convert the list of posts to an array and return!
                return questionList.ToArray();
            }
            else
            {
                //if they're not logged in, return an empty array
                return new questions[0];
            }
        }

        //EXAMPLE OF AN INSERT QUERY WITH PARAMS PASSED IN.  BONUS GETTING THE INSERTED ID FROM THE DB!
        [WebMethod(EnableSession = true)]
        public void GiveResponsesOne(string response)
        {
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["teamnala"].ConnectionString;
            //the only thing fancy about this query is SELECT LAST_INSERT_ID() at the end.  All that
            //does is tell mySql server to return the primary key of the last inserted row.
            string sqlInsert1 =
                "insert into surveyresponses (questionId, response) " +
                "values( " +
                "(select questionId from surveyquestions where questionId = (select max(questionId) - 1)), " +
                "@responseValue)";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlInsert1, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@responseValue", HttpUtility.UrlDecode(response));


            //this time, we're not using a data adapter to fill a data table.  We're just
            //opening the connection, telling our command to "executescalar" which says basically
            //execute the query and just hand me back the number the query returns (the ID, remember?).
            //don't forget to close the connection!
            sqlConnection.Open();
            //we're using a try/catch so that if the query errors out we can handle it gracefully
            //by closing the connection and moving on
            try
            {
                int accountID = Convert.ToInt32(sqlCommand.ExecuteScalar());
                //here, you could use this accountID for additional queries regarding
                //the requested account.  Really this is just an example to show you
                //a query where you get the primary key of the inserted row back from
                //the database!
            }
            catch (Exception e)
            {
            }
            sqlConnection.Close();
        }

        [WebMethod(EnableSession = true)]
        public void GiveResponsesTwo(int questionId, string questionCategory, string response)
        {
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["teamnala"].ConnectionString;
            //the only thing fancy about this query is SELECT LAST_INSERT_ID() at the end.  All that
            //does is tell mySql server to return the primary key of the last inserted row.
            string sqlInsert1 =
                "insert into surveyresponses (questionId, questionCategory, response) " +
                "values( " +
                "(select questionId from surveyquestions where questionId = (select max(questionId) - 1)), " +
                "(select questionCategory from surveyquestions where questionId = (select max(questionId) - 1)), " +
                "@responseValue)";


            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlInsert1, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@questionIdValue", HttpUtility.UrlDecode(Convert.ToString(questionId)));
            sqlCommand.Parameters.AddWithValue("@questionCategoryValue", HttpUtility.UrlDecode(questionCategory));
            sqlCommand.Parameters.AddWithValue("@responseValue", HttpUtility.UrlDecode(response));


            //this time, we're not using a data adapter to fill a data table.  We're just
            //opening the connection, telling our command to "executescalar" which says basically
            //execute the query and just hand me back the number the query returns (the ID, remember?).
            //don't forget to close the connection!
            sqlConnection.Open();
            //we're using a try/catch so that if the query errors out we can handle it gracefully
            //by closing the connection and moving on
            try
            {
                int accountID = Convert.ToInt32(sqlCommand.ExecuteScalar());
                //here, you could use this accountID for additional queries regarding
                //the requested account.  Really this is just an example to show you
                //a query where you get the primary key of the inserted row back from
                //the database!
            }
            catch (Exception e)
            {
            }
            sqlConnection.Close();
        }

    }
}
