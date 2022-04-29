﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Backend.Board;
using Backend.Exception;

namespace Backend.Move
{

    public class LegalMoveSet
    {

        private const int MAX_MOVE_COUNT = 27;

        private readonly List<(int, int)> Moves = new(MAX_MOVE_COUNT);
        
        private readonly DataBoard Board;
        private readonly (int, int) From;

        public LegalMoveSet(DataBoard board, (int, int) from, bool verify = true)
        {
            Board = board;
            From = from;
            (Piece piece, PieceColor color, _) = Board.At(from);
            
            // Generate Pseudo-Legal Moves
            switch (piece) {
                case Piece.Pawn:
                    if (verify) LegalPawnMoveSet(color);
                    else LegalPawnMoveSet(color, true);
                    break;
                case Piece.Rook:
                    LegalRookMoveSet(color);
                    break;
                case Piece.Knight:
                    LegalKnightMoveSet(color);
                    break;
                case Piece.Bishop:
                    LegalBishopMoveSet(color);
                    break;
                case Piece.Queen:
                    LegalQueenMoveSet(color);
                    break;
                case Piece.King:
                    LegalKingMoveSet(color);
                    break;
                case Piece.Empty:
                default:
                    throw InvalidMoveLookupException.FromBoard(
                        board, 
                        "Cannot generate move for empty piece: " + Util.TupleToChessString(from)
                    );
            }
            
            if (!verify) return;
            // Verify Pseudo-Legal Moves to ensure they're Legal Moves
            VerifyMoves(color);
        }

        public LegalMoveSet(DataBoard board, PieceColor color)
        {
            Board = board;

            foreach ((int, int) piece in Board.All(color)) {
                LegalMoveSet moveSet = new(board, piece);
                foreach ((int, int) move in moveSet.Get()) {
                    if (Moves.Contains(move)) continue;

                    Moves.Add(move);
                }
            }
        }

        public (int, int)[] Get()
        {
            return Moves.ToArray();
        }

        public int Count()
        {
            return Moves.Count;
        }

        private void LegalPawnMoveSet(PieceColor color, bool attackAllOnly = false)
        {
            (int h, int v) = From;

            int normalMoveC = Board.At(From).Item3 == MovedState.Moved ? 1 : 2;

            if (color == PieceColor.White) {
                // Normal
                if (!attackAllOnly)
                    for (int vI = v + 1; vI < DataBoard.UBOUND; vI++) {
                        if (normalMoveC == 0) break;

                        (int, int) move = (h, vI);
                        
                        if (!Board.EmptyAt(move)) break;
                        Moves.Add(move);

                        normalMoveC--;
                    }
                
                // Attack Move
                for (int hI = h - 1; hI < h + 2; hI++) {
                    if (hI is < 0 or > 7 || hI == h) continue;

                    (int, int) move = (hI, v + 1);
                    
                    if (Board.EmptyAt(move)) continue;
                    
                    (_, PieceColor otherColor, _) = Board.At(move);
                    if (color != otherColor) Moves.Add(move);
                }
                
                // En Passant
            } else {
                // Normal
                if (!attackAllOnly)
                    for (int vI = v - 1; vI > -1; vI--) {
                        if (normalMoveC == 0) break;

                        (int, int) move = (h, vI);
                        
                        if (!Board.EmptyAt(move)) break;
                        Moves.Add(move);

                        normalMoveC--;
                    }
                
                // Attack Move
                for (int hI = h - 1; hI < h + 2; hI++) {
                    if (hI is < 0 or > 7 || hI == h) continue;

                    (int, int) move = (hI, v - 1);
                    
                    if (Board.EmptyAt(move)) continue;
                    
                    (_, PieceColor otherColor, _) = Board.At(move);
                    if (color != otherColor) Moves.Add(move);
                }
                
                // En Passant
            }
        }

        private void LegalRookMoveSet(PieceColor color)
        {
            (int h, int v) = From;

            // fromInclusive, toExclusive, Iterator
            (int, int, int)[] loopDirection = {
                (h + 1, DataBoard.UBOUND, 1), // Right
                (h - 1, DataBoard.LBOUND, -1), // Left
                (v + 1, DataBoard.UBOUND, 1), // Top
                (v - 1, DataBoard.LBOUND, -1) // Bottom
            };

            int iteration = 0;
            foreach ((int, int, int) iterator in loopDirection) {
                if (iterator.Item2 == DataBoard.UBOUND)
                    for (int i = iterator.Item1; i < iterator.Item2; i += iterator.Item3) {
                        (int, int) move = iteration > 1 ? (h, i) : (i, v);

                        if (!AddMove(move, color)) break;
                    }
                else
                    for (int i = iterator.Item1; i > iterator.Item2; i += iterator.Item3) {
                        (int, int) move = iteration > 1 ? (h, i) : (i, v);

                        if (!AddMove(move, color)) break;
                    }

                iteration++;
            }
        }

        private void LegalKnightMoveSet(PieceColor color)
        {
            (int h, int v) = From;

            // Horizontal
            for (int hI = h - 2; hI < h + 3; hI++) {
                if (hI is < 0 or > 7) continue;
                if (hI != h - 2 && hI != h + 2) continue;

                for (int vI = v - 1; vI < v + 2; vI++) {
                    if (vI is < 0 or > 7 || vI == v) continue;

                    (int, int) move = (hI, vI);
                    AddMove(move, color);
                }
            }
            
            // Vertical
            for (int vI = v - 2; vI < v + 3; vI++) {
                if (vI is < 0 or > 7) continue;
                if (vI != v - 2 && vI != v + 2) continue;

                for (int hI = h - 1; hI < h + 2; hI++) {
                    if (hI is < 0 or > 7 || hI == h) continue;
                    
                    (int, int) move = (hI, vI);
                    AddMove(move, color);
                }
            }
        }

        private void LegalBishopMoveSet(PieceColor color)
        {
            (int h, int v) = From;

            (int, int)[] loopDirection =
            {
                (1, 1), // Top Right
                (-1, 1), // Top Left
                (-1, -1), // Bottom Left
                (1, -1) // Bottom Right
            };
            
            foreach ((int, int) iterator in loopDirection)
                for (int i = 1; i < DataBoard.UBOUND; i++) {
                    int hI = h + iterator.Item1 * i;
                    int vI = v + iterator.Item2 * i;
                    
                    if (hI is < 0 or > 7) break;
                    if (vI is < 0 or > 7) break;

                    (int, int) move = (hI, vI);
                    if (!AddMove(move, color)) break;
                }
        }

        private void LegalQueenMoveSet(PieceColor color)
        {
            LegalRookMoveSet(color);
            LegalBishopMoveSet(color);
        }

        private void LegalKingMoveSet(PieceColor color)
        {
            (int h, int v) = From;

            for (int hI = h - 1; hI < h + 2; hI++) {
                if (hI is < 0 or > 7) continue;
                for (int vI = v - 1; vI < v + 2; vI++) {
                    if (vI is < 0 or > 7) continue;
                    if (hI == h && vI == v) continue;

                    (int, int) move = (hI, vI);

                    AddMove(move, color);
                }
            }
        }

        private bool AddMove((int, int) move, PieceColor color)
        {
            if (!Board.EmptyAt(move)) {
                PieceColor otherColor = Board.At(move).Item2;
                if (color != otherColor) Moves.Add(move);
                return false;
            }
                        
            Moves.Add(move);
            return true;
        }

        private void VerifyMoves(PieceColor color)
        {
            PieceColor oppositeColor = Util.OppositeColor(color);
            
            List<(int, int)> verifiedMoves = new(27);
            foreach ((int, int) move in Moves) {
                DataBoard board = Board.Clone();
                board.Move(From, move);
                bool[,] bitBoard = board.BitBoard(oppositeColor);
                (int h, int v) = board.KingLoc(color);
                
                if (!bitBoard[h, v]) verifiedMoves.Add(move);
            }

            Moves.Clear();
            foreach ((int, int) move in verifiedMoves) {
                Moves.Add(move);
            }
        }

    }

}