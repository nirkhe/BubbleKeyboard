import java.io.File;
import java.io.FileNotFoundException;
import java.io.PrintStream;
import java.util.Scanner;


public class Runner
{

	public static void main(String[] args) throws FileNotFoundException
	{
		
		File f = new File("C:/users/chinmay/dropbox/kinect/texteval/texteval/TextTest/phrases.txt");
		File f1 = new File("C:/users/chinmay/dropbox/kinect/texteval/texteval/TextTest/bubble_phrases.txt");
		File f2 = new File("C:/users/chinmay/dropbox/kinect/texteval/texteval/TextTest/ptnclick_phrases.txt");
		
		Scanner s = new Scanner(f);
		PrintStream p1 = new PrintStream(f1);
		PrintStream p2 = new PrintStream(f2);
		
		while(s.hasNextLine())
		{
			String s_ = s.nextLine();
			String s_1 = "";
			for(char c: s_.toCharArray())
			{
				s_1 += Character.toLowerCase(c);			
			}
			s_1.toLowerCase();
			
			p1.println(s_1 + ' ');
			p2.println(s_1);
			
		}
		System.out.println("Complete!");
	}
	
}
