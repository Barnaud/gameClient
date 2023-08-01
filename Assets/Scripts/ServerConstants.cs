
public class ServerConstants {

    public static int tickrate = 30;
    public static float tick_duration = 1.0f / tickrate;

    //smtp_server
    /*    public static string url = "51.68.94.216";
        public static int server_port = 1826;*/

    //localhost
    public static string url = "127.0.0.1";
    public static int server_port = 1826;

    public static int local_port = 25567;
    public static int saved_player_positions = 20;
    public static float reconciliation_distance_treshold = 0;
    public static int faulty_ticks_before_reconciliation = 5;
}
