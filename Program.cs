using System.Text.Json;

namespace movies
{
    class Program
    {
        static MovieInfo movieInfo = new MovieInfo() { filename = "movie1", x = 4, y = 4, playBackSpeed = 1 };

        static void Main(string[] args)
        {
            Movie movie = new Movie("movie1");

            movie.Navigate();
            // movie.Play(Axis.x);
        }
    }

    class Movie
    {
        public MovieInfo info;
        public int[,,] frames;

        public Movie(string filename)
        {
            FileInfo fileInfo = new FileInfo(filename);

            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("file not found", filename);
            }

            MovieData d = Load(fileInfo.OpenRead());

            info = d.info;
            frames = d.frames;
        }

        public void Export()
        {
            using (Stream s = File.OpenWrite(info.filename))
            {
                s.Write(JsonSerializer.SerializeToUtf8Bytes<FileMovieData>(
                    FileMovieData.Parse(new MovieData() { frames = frames, info = info }),
                    new JsonSerializerOptions() { WriteIndented = true })
                    );
            }
        }

        public static void Export(MovieData data)
        {
            using (Stream s = File.OpenWrite(data.info.filename))
            {
                s.Write(JsonSerializer.SerializeToUtf8Bytes<FileMovieData>(
                    FileMovieData.Parse(data),
                    new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    }
                    )
                );
            }
        }

        public static MovieData Load(Stream fileStream, bool closeStream = true)
        {
            byte[] buffer = new byte[111000];

            int size = fileStream.Read(buffer);

            FileMovieData data = JsonSerializer.Deserialize<FileMovieData>(buffer.Take(size).ToArray());

            if (closeStream)
            {
                fileStream.Close();
                fileStream.Dispose();
            }

            return MovieData.Parse(data);
        }

        public void Play(Axis timelineAxis)
        {
            Console.Clear();

            for (int i = 0; i < frames.GetLength((int)timelineAxis); i++)
            {
                int[,] frame = GetAtAxis(timelineAxis, i);

                Draw(frame);

                Thread.Sleep(info.playBackSpeed * 1000); // s
            }

            Console.BackgroundColor = ConsoleColor.Black;
        }

        public void Navigate()
        {
            ConsoleKeyInfo keyInfo;

            int currentAxis = 0;
            int currentFrame = 0;

            Console.Clear();

            do
            {
                Draw(GetAtAxis((Axis)(currentAxis % 3), (currentFrame % frames.GetLength(currentAxis % 3))));

                Console.BackgroundColor = ConsoleColor.Black;
                Console.SetCursorPosition(0, Console.WindowHeight - 2);
                Console.WriteLine("timeline axis: {0}", Enum.GetName<Axis>((Axis)(currentAxis % 3)));
                Console.Write("frame: {0}", currentFrame % frames.GetLength(currentAxis % 3));

                keyInfo = Console.ReadKey();

                switch (keyInfo.Key)
                {
                    case ConsoleKey.UpArrow: // axis + 1
                        currentAxis++;
                        currentAxis = Math.Max(0, currentAxis);
                        break;

                    case ConsoleKey.DownArrow: // axis - 1
                        currentAxis--;
                        currentAxis = Math.Max(0, currentAxis);
                        break;

                    case ConsoleKey.LeftArrow: // previous frame
                        currentFrame--;
                        currentFrame = Math.Max(0, currentFrame);
                        break;

                    case ConsoleKey.RightArrow: // next frame
                        currentFrame++;
                        currentFrame = Math.Max(0, currentFrame);
                        break;
                }

            } while (keyInfo.Key != ConsoleKey.Enter);

            Console.BackgroundColor = ConsoleColor.Black;
        }

        public void Draw(int[,] frame, int pixelHeight = 3, int pixelWidth = 4, bool debug = false)
        {
            for (int x = 0; x < frame.GetLength((int)Axis.x); x++)
            {
                for (int y = 0; y < frame.GetLength((int)Axis.y); y++)
                {
                    Console.BackgroundColor = (ConsoleColor)frame[x, y];
                    if (debug)
                    {
                        Console.BackgroundColor = (ConsoleColor)(frame[x, y] == 0 ? 9 : frame[x, y]);
                    }

                    foreach (int pixelX in Enumerable.Range(0, pixelWidth))
                    {
                        foreach (int pixelY in Enumerable.Range(0, pixelHeight))
                        {
                            Console.SetCursorPosition(x * pixelWidth + pixelX, y * pixelHeight + pixelY);
                            Console.Write(" ");
                        }
                    }


                }
            }
        }

        public int[,] GetAtAxis(Axis axis, int pos)
        {
            int[,] result;

            //initialize result
            if (axis == Axis.x)
            {
                result = new int[frames.GetLength((int)Axis.y), frames.GetLength((int)Axis.z)];
            }
            else if (axis == Axis.y)
            {
                result = new int[frames.GetLength((int)Axis.x), frames.GetLength((int)Axis.z)];
            }
            else // axis == Axis.z
            {
                result = new int[frames.GetLength((int)Axis.x), frames.GetLength((int)Axis.y)];
            }


            // fill in result
            if (axis == Axis.x)
            {
                for (int y = 0; y < frames.GetLength((int)Axis.y); y++)
                {
                    for (int z = 0; z < frames.GetLength((int)Axis.z); z++)
                    {
                        result[y, z] = frames[pos, y, z];
                    }
                }
            }
            else if (axis == Axis.y)
            {
                for (int x = 0; x < frames.GetLength((int)Axis.x); x++)
                {
                    for (int z = 0; z < frames.GetLength((int)Axis.z); z++)
                    {
                        result[x, z] = frames[x, pos, z];
                    }
                }
            }
            else // axis == Axis.z
            {
                for (int x = 0; x < frames.GetLength((int)Axis.x); x++)
                {
                    for (int y = 0; y < frames.GetLength((int)Axis.y); y++)
                    {
                        result[x, y] = frames[x, y, pos];
                    }
                }
            }

            return result;
        }
    }

    struct FileMovieData
    {
        public static int[][][] ParseFrames(int[,,] frames)
        {
            int[][][] result = new int[frames.GetLength((int)Axis.x)][][];

            for (int x = 0; x < frames.GetLength((int)Axis.x); x++)
            {
                result[x] = new int[frames.GetLength((int)Axis.y)][];

                for (int y = 0; y < frames.GetLength((int)Axis.y); y++)
                {
                    result[x][y] = new int[frames.GetLength((int)Axis.z)];
                }
            }

            for (int x = 0; x < frames.GetLength((int)Axis.x); x++)
            {
                for (int y = 0; y < frames.GetLength((int)Axis.y); y++)
                {
                    for (int z = 0; z < frames.GetLength((int)Axis.z); z++)
                    {
                        result[x][y][z] = frames[x, y, z];
                    }
                }
            }

            return result;
        }

        public static FileMovieData Parse(MovieData data)
        {
            return new FileMovieData()
            {
                frames = ParseFrames(data.frames),
                info = data.info
            };
        }

        public MovieInfo info { get; set; }
        public int[][][] frames { get; set; }
    }

    struct MovieData
    {
        public static MovieData Parse(FileMovieData data)
        {
            int[,,] newFrames = new int[data.info.x, data.info.y, data.frames[0][0].Length];

            for (int x = 0; x < newFrames.GetLength((int)Axis.x); x++)
            {
                for (int y = 0; y < newFrames.GetLength((int)Axis.y); y++)
                {
                    for (int z = 0; z < newFrames.GetLength((int)Axis.z); z++)
                    {
                        newFrames[x, y, z] = data.frames[x][y][z];
                    }
                }
            }

            return new MovieData()
            {
                frames = newFrames,
                info = data.info
            };
        }

        public MovieInfo info;
        public int[,,] frames;
    }

    enum Axis
    {
        x = 0,
        y = 1,
        z = 2
    }

    struct MovieInfo
    {
        public int x { get; set; }
        public int y { get; set; }
        public int playBackSpeed { get; set; }
        public string filename { get; set; }
    }
}