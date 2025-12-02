using OpenCvSharp;

namespace RemTechAvitoVehiclesParser.Parsing.FirewallBypass;

public sealed class AvitoCaptchaMovementPosition(AvitoCaptchaImages images)
{
    public int CenterPoint()
    {
        byte[] backGround = images.ReadMax();
        byte[] puzzlePiece = images.ReadMin();
        return CalculateCenterPointForBackground(CreateBackgroundImage(backGround), CreatePuzzleImage(puzzlePiece));
    }

    private static int CalculateCenterPointForBackground(Mat background, Mat puzzlePiece)
    {
        try
        {
            Mat edgePuzzlePiece = CreateEdgePuzzlePiece(puzzlePiece);
            Mat edgeBackground = CreateEdgeBackground(background);
            Mat edgePuzzlePieceRgb = CreateEdgePuzzlePieceRgb(edgePuzzlePiece);
            Mat edgeBackgroundRgb = CreateEdgeBackgroundRgb(edgeBackground);
            int centerPoint = GetCenterPoint(edgeBackgroundRgb, edgePuzzlePieceRgb, edgePuzzlePieceRgb);
            edgePuzzlePiece.Dispose();
            edgeBackgroundRgb.Dispose();
            edgePuzzlePieceRgb.Dispose();
            edgeBackgroundRgb.Dispose();
            background.Dispose();
            puzzlePiece.Dispose();
            return centerPoint;
        }
        catch
        {
            background.Dispose();
            puzzlePiece.Dispose();
            throw;
        }
    }

    private Mat CreateBackgroundImage(byte[] bytes)
    {
        Mat result = Cv2.ImDecode(bytes, ImreadModes.Color);
        return result;
    }
    
    private Mat CreatePuzzleImage(byte[] bytes)
    {
        Mat result = Cv2.ImDecode(bytes, ImreadModes.Color);
        return result;
    }
    
    private static Mat CreateEdgePuzzlePiece(Mat puzzlePiece)
    {
        Mat edgePuzzlePiece = new();
        Cv2.Canny(puzzlePiece, edgePuzzlePiece, 100, 200);
        return edgePuzzlePiece;
    }

    private static Mat CreateEdgeBackground(Mat background)
    {
        Mat edgeBackground = new();
        Cv2.Canny(background, edgeBackground, 100, 200);
        return edgeBackground;
    }

    private static Mat CreateEdgePuzzlePieceRgb(Mat edgePuzzlePiece)
    {
        Mat edgePuzzlePieceRgb = new();
        Cv2.CvtColor(edgePuzzlePiece, edgePuzzlePieceRgb, ColorConversionCodes.BGR2RGB);
        return edgePuzzlePieceRgb;
    }

    private static Mat CreateEdgeBackgroundRgb(Mat edgeBackground)
    {
        Mat edgeBackgroundRgb = new();
        Cv2.CvtColor(edgeBackground, edgeBackgroundRgb, ColorConversionCodes.BGR2RGB);
        return edgeBackgroundRgb;
    }
    
    private static int GetCenterPoint(
        Mat edgeBackgroundRgb,
        Mat edgePuzzlePieceRgb,
        Mat edgePuzzlePiece)
    {
        Mat result = new();

        Cv2.MatchTemplate(
            edgeBackgroundRgb,
            edgePuzzlePieceRgb,
            result,
            TemplateMatchModes.CCoeffNormed);

        Cv2.MinMaxLoc(result, out _, out _, out _, out Point maxLoc);

        int w = edgePuzzlePiece.Cols;
        
        int centerX = maxLoc.X + (w / 2);

        result.Dispose();
        return centerX;
    }
}